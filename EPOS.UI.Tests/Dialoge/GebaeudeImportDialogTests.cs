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
        Enumerable.Range(0, 13).Select(i => "Klasse " + (char)('A' + i)).ToList();   // E47: A bis M

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
        bool ohneUebernehmen = false,
        Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>? zuordnen = null)
    {
        GebaeudeImportProfilDaten pr = profil ?? ProfilA;
        Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand> stand = zuordnen ?? Stand;
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
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return stand(a); }));
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
        Assert.Equal(14, erstesFeld.QuerySelectorAll("option").Length);   // „— keine —" und 13 Klassen
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

    /// <summary>
    /// <b>Das Baujahr der Datei führt</b> (E47, F2): Trägt die Datei ein Baujahr, zeigt die Klappliste
    /// die Klasse daraus, ist gesperrt und nennt darunter den Hinweis der Datenseite; ohne Baujahr ist
    /// die Klasse wählbar und geht in die Anfrage.
    /// </summary>
    [Fact]
    public void Mit_Baujahr_zeigt_die_Klappliste_die_Klasse_der_Datei_und_ist_gesperrt()
    {
        var p = new Protokoll();
        GebaeudeImportStand MitBaujahr(GebaeudeZuordnungsanfrage a) => Stand(a) with
        {
            KlasseDerDatei = 4,
            KlassenHinweis = "Hinweis aus dem Baujahr",
        };
        var cut = Bauen(p, zuordnen: MitBaujahr);
        Einlesen(cut);

        IElement klasse = cut.FindAll(".epos-feld")[0].QuerySelector("select")!;
        Assert.Equal("4", klasse.GetAttribute("value"));
        Assert.True(klasse.HasAttribute("disabled"));
        Assert.Contains("Hinweis aus dem Baujahr", cut.Markup);
        Assert.Null(p.Anfragen.Last().Baualtersklasse);
    }

    [Fact]
    public void Ohne_Baujahr_ist_die_Klasse_waehlbar()
    {
        var p = new Protokoll();
        GebaeudeImportStand OhneBaujahr(GebaeudeZuordnungsanfrage a) => Stand(a) with
        {
            KlasseDerDatei = null,
            KlassenHinweis = a.Baualtersklasse is null ? "Hinweis ohne Klasse" : "Hinweis zur Wahl",
        };
        var cut = Bauen(p, zuordnen: OhneBaujahr);
        Einlesen(cut);

        IElement klasse = cut.FindAll(".epos-feld")[0].QuerySelector("select")!;
        Assert.False(klasse.HasAttribute("disabled"));
        Assert.Contains("Hinweis ohne Klasse", cut.Markup);

        klasse.Change("4");   // Klasse E
        cut.WaitForAssertion(() => Assert.Equal(4, p.Anfragen.Last().Baualtersklasse));
        Assert.Equal("4", cut.FindAll(".epos-feld")[0].QuerySelector("select")!.GetAttribute("value"));
        Assert.Contains("Hinweis zur Wahl", cut.Markup);
    }

    /// <summary>
    /// <b>Eine Folgevorgabe zieht nach</b>: Die Datenseite rechnet die inneren Gewinne aus der
    /// Nutzfläche der Handwerte (hier 5 W/m² wie im Kern); nach einer Flächenänderung zeigt der Dialog
    /// den neuen Wert der Gewinne, deren Herkunft Vorgabe bleibt.
    /// </summary>
    [Fact]
    public void Nach_einer_Flaechenaenderung_zeigt_der_Dialog_die_nachgezogenen_Gewinne()
    {
        var p = new Protokoll();
        GebaeudeImportStand MitGewinnen(GebaeudeZuordnungsanfrage a)
        {
            GebaeudeImportStand s = Stand(a);
            double flaeche = a.Handwerte is { } h && h.TryGetValue("NUTZ", out double? w) && w.HasValue ? w.Value : 50;
            GebaeudeFeldzeileDaten gewinne = Zeile("GEW", "Kenngrößen", "Interne Wärmegewinne", 5 * flaeche, "W", haken: true, eingebbar: true)
                with { HerkunftText = "HK-Vorgabe", HerkunftSchluessel = "VORGABE" };
            return s with { Zeilen = s.Zeilen.Take(1).Append(gewinne).Concat(s.Zeilen.Skip(1)).ToList() };
        }
        var cut = Bauen(p, zuordnen: MitGewinnen);
        Einlesen(cut);
        Assert.Equal("250", ZeileVon(cut, "GEW").QuerySelector("input.epos-eingabe")!.GetAttribute("value"));

        ZeileVon(cut, "NUTZ").QuerySelector("input.epos-eingabe")!.Input("80");

        cut.WaitForAssertion(() => Assert.Equal("400", ZeileVon(cut, "GEW").QuerySelector("input.epos-eingabe")!.GetAttribute("value")));
        Assert.Contains("HK-Vorgabe", ZeileVon(cut, "GEW").TextContent);     // die Gewinne bleiben Vorgabe
        Assert.Contains("Hand-Probe", ZeileVon(cut, "NUTZ").TextContent);    // die Fläche ist manuell

        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.Equal(400, p.Geschlossen[0]!.Zeile("GEW")!.Wert);
        Assert.Equal("VORGABE", p.Geschlossen[0]!.Zeile("GEW")!.HerkunftSchluessel);
    }

    // =====================================================================
    //  Der Abschnitt „Bauteile (echte Hülle)" (Stufe G4b)
    // =====================================================================

    /// <summary>Ein Bauteilvorschlag, wie die Hülle ihn reicht: übernehmbar oder mit Grund abgelehnt.</summary>
    private static GebaeudeBauteileDaten Bauteile(bool moeglich, int zeilen = 3)
        => new()
        {
            Moeglich = moeglich,
            Ablehnung = moeglich ? "" : "Ablehnung-Probe",
            Kopftext = "Zone-Probe · " + zeilen + " Bauteile",
            Zeilen = Enumerable.Range(1, zeilen)
                .Select(i => new GebaeudeBauteilzeileDaten("Bauteil " + i, "Art-Probe", i + " m²", i == 1 ? "aus Schichten" : "0,3 W/(m²K)",
                                                           "180°", "90°", "Rand-Probe", "HK-Datei", "DATEI", "k-" + i))
                .ToList(),
            Innenweg = "Innenweg-Probe",
            Meldungen = new[] { new GebaeudeImportMeldung(WarnStufe.Hinweis, "Info", "Bauteilmeldung-Probe", "IMP_BAUTEIL_PROT_X") },
        };

    /// <summary>
    /// Die Datenseite bildet den Vorschlag bei jeder Anfrage neu — hier hängt er an den Raumhaken:
    /// Ist der Keller (r-2) beheizt, lässt er sich nicht bilden; sonst drei Zeilen.
    /// </summary>
    private static GebaeudeImportStand MitBauteilen(GebaeudeZuordnungsanfrage a)
    {
        bool keller = a.BeheiztUebersteuert.TryGetValue("r-2", out bool b) && b;
        return Stand(a) with { Bauteile = Bauteile(!keller, keller ? 0 : 3) };
    }

    private static IElement Zonenschalter(IRenderedComponent<GebaeudeImportDialog> cut)
        => cut.Find(".epos-gebimport-bauteile input[type=checkbox]");

    [Fact]
    public void Der_Abschnitt_Bauteile_zeigt_den_vorbelegten_Schalter_die_Liste_den_Innenweg_und_die_Meldungen()
    {
        var p = new Protokoll();
        var cut = Bauen(p, zuordnen: MitBauteilen);
        Einlesen(cut);

        Assert.Contains(cut.FindAll("h2, h3, .epos-gruppenkopf"), k => k.TextContent.Contains("Bauteile (echte Hülle)"));
        IElement schalter = Zonenschalter(cut);
        Assert.True(schalter.HasAttribute("checked"));
        Assert.False(schalter.HasAttribute("disabled"));
        Assert.Contains("Als Zone mit Bauteilen übernehmen", cut.Find(".epos-gebimport-bauteile").TextContent);
        Assert.True(cut.Instance.AlsZoneWirksam);
        Assert.Empty(cut.FindAll(".epos-gebimport-bauteile-ablehnung"));

        IReadOnlyList<IElement> zeilen = cut.FindAll(".epos-gebimport-bauteilliste tbody tr");
        Assert.Equal(3, zeilen.Count);
        IReadOnlyList<string> zellen = zeilen[0].QuerySelectorAll("td").Select(t => t.TextContent).ToList();
        Assert.Equal(new[] { "Bauteil 1", "Art-Probe", "1 m²", "aus Schichten", "180°", "90°", "Rand-Probe", "HK-Datei" }, zellen);
        Assert.Equal("k-1", zeilen[0].GetAttribute("data-kennung"));
        Assert.Contains("Bauteil", cut.Find(".epos-gebimport-bauteilliste thead").TextContent);
        Assert.Contains("Zone-Probe", cut.Find(".epos-gebimport-bauteile-kopf").TextContent);
        Assert.Equal("Innenweg-Probe", cut.Find(".epos-gebimport-innenweg").TextContent);
        Assert.Contains("Bauteilmeldung-Probe", cut.Find(".epos-gebimport-bauteilmeldungen").TextContent);
        // Die Meldungen des Vorschlags stehen im Abschnitt, nicht in den Meldungen der Zuordnung.
        Assert.Single(cut.FindAll(".epos-gebimport-meldungen tbody tr"));

        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.True(p.Geschlossen[0]!.AlsZone);
        Assert.True(Assert.Single(p.Uebernommen).AlsZone);
    }

    [Fact]
    public void Ohne_Schalter_kommt_das_Gebaeude_nur_mit_den_Summen()
    {
        var p = new Protokoll();
        var cut = Bauen(p, zuordnen: MitBauteilen);
        Einlesen(cut);

        Zonenschalter(cut).Change(false);
        cut.WaitForAssertion(() => Assert.False(cut.Instance.AlsZoneWirksam));
        // Ein Neuzuordnen (Klassenwechsel) behält die Wahl.
        cut.FindAll(".epos-feld")[0].QuerySelector("select")!.Change("4");
        cut.WaitForAssertion(() => Assert.Equal(2, p.Anfragen.Count));
        Assert.False(Zonenschalter(cut).HasAttribute("checked"));

        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.False(p.Geschlossen[0]!.AlsZone);
    }

    [Fact]
    public void Ein_abgelehnter_Vorschlag_sperrt_den_Schalter_und_nennt_den_Grund_der_Raumhaken_bildet_ihn_neu()
    {
        var p = new Protokoll();
        var cut = Bauen(p, zuordnen: MitBauteilen);
        Einlesen(cut);
        Assert.Equal(3, cut.FindAll(".epos-gebimport-bauteilliste tbody tr").Count);

        // Der Keller als beheizt: Die Datenseite bildet den Vorschlag neu — er lässt sich nicht bilden.
        cut.FindAll(".epos-gebimport-raeume tbody tr")[1].QuerySelector("input[type=checkbox]")!.Change(true);
        cut.WaitForAssertion(() => Assert.True(Zonenschalter(cut).HasAttribute("disabled")));
        Assert.False(Zonenschalter(cut).HasAttribute("checked"));
        Assert.Equal("Nicht möglich: Ablehnung-Probe", cut.Find(".epos-gebimport-bauteile-ablehnung").TextContent);
        Assert.Empty(cut.FindAll(".epos-gebimport-bauteilliste"));
        Assert.Equal("Keine Bauteile.", cut.Find(".epos-gebimport-bauteile-leer").TextContent);
        Assert.False(cut.Instance.AlsZoneWirksam);

        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.False(p.Geschlossen[0]!.AlsZone);

        // Wieder unbeheizt: der Vorschlag ist zurück, der Schalter wieder vorbelegt „ein".
        var p2 = new Protokoll();
        var cut2 = Bauen(p2, zuordnen: MitBauteilen);
        Einlesen(cut2);
        cut2.FindAll(".epos-gebimport-raeume tbody tr")[1].QuerySelector("input[type=checkbox]")!.Change(true);
        cut2.WaitForAssertion(() => Assert.True(Zonenschalter(cut2).HasAttribute("disabled")));
        cut2.FindAll(".epos-gebimport-raeume tbody tr")[1].QuerySelector("input[type=checkbox]")!.Change(false);
        cut2.WaitForAssertion(() => Assert.False(Zonenschalter(cut2).HasAttribute("disabled")));
        Assert.True(Zonenschalter(cut2).HasAttribute("checked"));
    }

    [Fact]
    public void Ohne_Vorschlag_steht_kein_Abschnitt_Bauteile()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);
        Assert.Empty(cut.FindAll(".epos-gebimport-bauteile"));
        Assert.DoesNotContain("Bauteile (echte Hülle)", cut.Markup);
        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.False(p.Geschlossen[0]!.AlsZone);
    }

    [Fact]
    public void Die_Texte_des_Abschnitts_stehen_auch_englisch()
    {
        GebaeudeImportTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeImportTexte();
        Assert.Equal("Components (real envelope)", englisch.GruppeBauteile);
        Assert.Equal("Take over as a zone with components", englisch.AlsZone);
        Assert.Equal("Not possible: {0}", englisch.AlsZoneNicht);

        var p = new Protokoll();
        var cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, ProfilA);
            c.Add(x => x.Baualtersklassen, Klassen);
            c.Add(x => x.Texte, englisch);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)((_, _, _) =>
                Task.FromResult(Gelesen())));
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return MitBauteilen(a); }));
        });
        cut.FindAll("button").First(k => k.TextContent.Contains(englisch.DateiKnopf.TrimEnd('…', '.'))).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-bauteilliste tbody tr")));

        Assert.Contains("Components (real envelope)", cut.Markup);
        Assert.Contains("Take over as a zone with components", cut.Find(".epos-gebimport-bauteile").TextContent);
        string kopf = cut.Find(".epos-gebimport-bauteilliste thead").TextContent;
        foreach (string spalte in new[] { "Component", "Type", "U-value", "Azimuth", "Tilt", "Boundary condition" })
            Assert.Contains(spalte, kopf);
    }

    // =====================================================================
    //  Der Abschnitt „Baustoffe" (Namensabgleich)
    // =====================================================================

    private static readonly IReadOnlyList<GebaeudeBaustoffgruppe> BaustoffKatalog = new[]
    {
        new GebaeudeBaustoffgruppe("Putze", new[] { new GebaeudeBaustoffwahl(1, "Kalkzementputz"), new GebaeudeBaustoffwahl(2, "Gipsputz 1200") }),
        new GebaeudeBaustoffgruppe("Estriche", new[] { new GebaeudeBaustoffwahl(5, "Zementestrich"), new GebaeudeBaustoffwahl(1001, "Fließestrich (Hersteller A)") }),
    };

    private static string KatalogText(int id) => BaustoffKatalog.SelectMany(g => g.Eintraege).Single(e => e.Id == id).Text;

    /// <summary>
    /// Eine Materialzeile, wie die Datenseite sie bildet: die Zuordnung des Dialogs vor der gemerkten,
    /// die gemerkte vor dem Abgleich; <c>null</c> im Dialog nimmt die gemerkte weg.
    /// </summary>
    private static GebaeudeMaterialzeileDaten Materialzeile(IReadOnlyDictionary<string, int?> dialog, IReadOnlyDictionary<string, int> gemerkt,
                                                            string name, string schluessel, int? auto, string autoSchluessel, string autoText)
    {
        int? eigen = dialog.TryGetValue(schluessel, out int? d) ? d : gemerkt.TryGetValue(schluessel, out int g) ? g : null;
        int? id = eigen ?? auto;
        string stufe = eigen.HasValue ? GebaeudeAbgleichSchluessel.EigeneZuordnung : autoSchluessel;
        return new GebaeudeMaterialzeileDaten
        {
            Name = name, Schluessel = schluessel, Schichten = 2,
            Abgleich = eigen.HasValue ? "eigene Zuordnung" : autoText, AbgleichSchluessel = stufe, Beleg = "Beleg-" + schluessel,
            IdBaustoff = id, Baustoff = id is int i ? KatalogText(i) : "", Stoffwerte = id is null ? "" : "λ-Probe",
            Werte = id is null ? "ohne Aufbau" : "aus dem Katalog",
            OhneTreffer = id is null && stufe == GebaeudeAbgleichSchluessel.Ohne,
            Gemerkt = gemerkt.ContainsKey(schluessel), Vorgemerkt = dialog.ContainsKey(schluessel),
        };
    }

    /// <summary>
    /// Der Stand mit dem Abschnitt „Baustoffe": Gipsputz trifft über den Wortanfang, Fußbodenaufbau
    /// nichts, Air ist eine Luftschicht. Die Datenseite bildet den Vorschlag neu — mit einem Baustoff
    /// für Fußbodenaufbau hat er einen Aufbau mehr.
    /// </summary>
    private static GebaeudeImportStand MitBaustoffen(GebaeudeZuordnungsanfrage a, IReadOnlyDictionary<string, int>? gemerkt = null)
    {
        IReadOnlyDictionary<string, int?> dialog = a.Baustoffzuordnungen ?? new Dictionary<string, int?>();
        gemerkt ??= new Dictionary<string, int>();
        GebaeudeMaterialzeileDaten[] zeilen =
        {
            Materialzeile(dialog, gemerkt, "Gipsputz", "gipsputz", 2, "N5", "Wortanfang"),
            Materialzeile(dialog, gemerkt, "Fußbodenaufbau", "fussbodenaufbau", null, GebaeudeAbgleichSchluessel.Ohne, "ohne Treffer"),
            Materialzeile(dialog, gemerkt, "Air", "air", null, "LUFTSCHICHT", "Luftschicht"),
        };
        int aufbauten = 6 + (zeilen[1].IdBaustoff.HasValue ? 1 : 0);
        return Stand(a) with
        {
            Bauteile = Bauteile(true) with { Kopftext = "Zone-Probe · " + aufbauten + " Aufbauten" },
            Baustoffe = new GebaeudeBaustoffeDaten
            {
                Zusammenfassung = zeilen.Count(z => z.IdBaustoff.HasValue) + " von 3 zugeordnet, " + zeilen.Count(z => z.OhneTreffer) + " ohne Treffer",
                Zeilen = zeilen,
                Katalog = BaustoffKatalog,
            },
        };
    }

    private static IElement Baustoffzeile(IRenderedComponent<GebaeudeImportDialog> cut, string schluessel)
        => cut.Find(".epos-gebimport-baustoffe tr[data-schluessel=\"" + schluessel + "\"]");

    private static IReadOnlyList<string> Zellen(IElement zeile)
        => zeile.QuerySelectorAll("td").Select(t => t.TextContent.Trim()).ToList();

    [Fact]
    public void Der_Abschnitt_Baustoffe_steht_nur_mit_Materialnamen()
    {
        var ohne = Bauen(new Protokoll(), zuordnen: MitBauteilen);
        Einlesen(ohne);
        Assert.Empty(ohne.FindAll(".epos-gebimport-baustoffe"));
        Assert.DoesNotContain(">Baustoffe<", ohne.Markup);

        var cut = Bauen(new Protokoll(), zuordnen: a => MitBaustoffen(a));
        Einlesen(cut);
        Assert.Contains(cut.FindAll("h2, h3, .epos-gruppenkopf"), k => k.TextContent.Trim() == "Baustoffe");
        Assert.Equal(3, cut.FindAll(".epos-gebimport-baustoffe tbody tr").Count);
        Assert.Equal("1 von 3 zugeordnet, 1 ohne Treffer", cut.Find(".epos-gebimport-baustoffe-summe").TextContent);
        string kopf = cut.Find(".epos-gebimport-baustoffe thead").TextContent;
        foreach (string spalte in new[] { "Name in der Datei", "Schichten", "Zuordnung über", "Baustoff", "Stoffwerte", "Werte" })
            Assert.Contains(spalte, kopf);
        // Der Abschnitt steht unter den Bauteilen und vor den Meldungen.
        string markup = cut.Markup;
        Assert.True(markup.IndexOf("epos-gebimport-bauteilliste", StringComparison.Ordinal) < markup.IndexOf("epos-gebimport-baustoffe", StringComparison.Ordinal));
        Assert.True(markup.IndexOf("epos-gebimport-baustoffe", StringComparison.Ordinal) < markup.IndexOf("epos-gebimport-meldungen", StringComparison.Ordinal));
    }

    [Fact]
    public void Die_Zeilen_zeigen_Stufe_Baustoff_und_Werte_und_ohne_Treffer_ist_gelb()
    {
        var cut = Bauen(new Protokoll(), zuordnen: a => MitBaustoffen(a));
        Einlesen(cut);

        IElement gips = Baustoffzeile(cut, "gipsputz");
        IReadOnlyList<string> zellen = Zellen(gips);
        Assert.Equal(new[] { "Gipsputz", "2", "Wortanfang" }, zellen.Take(3));
        Assert.Equal(new[] { "λ-Probe", "aus dem Katalog" }, zellen.Skip(4).Take(2));
        Assert.Equal("Beleg-gipsputz", gips.QuerySelector(".epos-gebimport-abgleich")!.GetAttribute("title"));
        Assert.Contains("epos-gebimport-abgleich--n5", gips.QuerySelector(".epos-gebimport-abgleich")!.ClassName);
        IElement wahl = gips.QuerySelector("select")!;
        Assert.Equal("Baustoff für „Gipsputz“", wahl.GetAttribute("aria-label"));
        Assert.Equal(new[] { "Putze", "Estriche" }, wahl.QuerySelectorAll("optgroup").Select(g => g.GetAttribute("label")));
        IElement gewaehlt = Assert.Single(wahl.QuerySelectorAll("option"), o => o.HasAttribute("selected"));
        Assert.Equal(("2", "Gipsputz 1200"), (gewaehlt.GetAttribute("value"), gewaehlt.TextContent));
        Assert.Empty(wahl.QuerySelectorAll("option[value='']"));                 // mit Baustoff keine leere Zeile
        Assert.Contains("Fließestrich (Hersteller A)", wahl.TextContent);
        Assert.Null(gips.QuerySelector(".epos-gebimport-entfernen"));             // nur eine eigene Zuordnung lässt sich entfernen
        Assert.DoesNotContain("epos-gebimport-zeile--gelb", gips.ClassName ?? "");

        IElement fb = Baustoffzeile(cut, "fussbodenaufbau");
        Assert.Contains("epos-gebimport-zeile--gelb", fb.ClassName);
        Assert.Equal("ohne Treffer", Zellen(fb)[2]);
        IElement leer = Assert.Single(fb.QuerySelectorAll("option"), o => o.HasAttribute("selected"));
        Assert.Equal(("", "(Baustoff wählen)"), (leer.GetAttribute("value"), leer.TextContent));

        IElement luft = Baustoffzeile(cut, "air");
        Assert.Equal("Luftschicht", Zellen(luft)[2]);
        Assert.DoesNotContain("epos-gebimport-zeile--gelb", luft.ClassName ?? "");
    }

    [Fact]
    public void Die_Auswahl_setzt_den_Baustoff_und_bildet_den_Vorschlag_neu()
    {
        var p = new Protokoll();
        var cut = Bauen(p, zuordnen: a => MitBaustoffen(a));
        Einlesen(cut);
        Assert.Contains("6 Aufbauten", cut.Find(".epos-gebimport-bauteile-kopf").TextContent);

        Baustoffzeile(cut, "fussbodenaufbau").QuerySelector("select")!.Change("5");
        cut.WaitForAssertion(() => Assert.Equal(2, p.Anfragen.Count));
        Assert.Equal(new Dictionary<string, int?> { ["fussbodenaufbau"] = 5 }, p.Anfragen[1].Baustoffzuordnungen);
        // Der Vorschlag ist neu gebildet: ein Aufbau mehr; die Zeile trägt die eigene Zuordnung.
        Assert.Contains("7 Aufbauten", cut.Find(".epos-gebimport-bauteile-kopf").TextContent);
        IElement fb = Baustoffzeile(cut, "fussbodenaufbau");
        Assert.Equal("eigene Zuordnung", Zellen(fb)[2]);
        Assert.DoesNotContain("epos-gebimport-zeile--gelb", fb.ClassName);
        Assert.Contains("epos-gebimport-baustoff--vorgemerkt", fb.ClassName);
        Assert.Equal("5", Assert.Single(fb.QuerySelectorAll("option"), o => o.HasAttribute("selected")).GetAttribute("value"));
        Assert.NotNull(fb.QuerySelector(".epos-gebimport-entfernen"));
        Assert.Equal("2 von 3 zugeordnet, 0 ohne Treffer", cut.Find(".epos-gebimport-baustoffe-summe").TextContent);

        // Ein Klassenwechsel ordnet neu zu und behält die Zuordnung.
        cut.FindAll(".epos-feld")[0].QuerySelector("select")!.Change("4");
        cut.WaitForAssertion(() => Assert.Equal(3, p.Anfragen.Count));
        Assert.Equal(5, p.Anfragen[2].Baustoffzuordnungen!["fussbodenaufbau"]);

        // Nichts wird vor dem OK geschrieben; das Ergebnis trägt die Zuordnung.
        Assert.Empty(p.Uebernommen);
        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.Equal(new Dictionary<string, int?> { ["fussbodenaufbau"] = 5 }, p.Geschlossen[0]!.Baustoffzuordnungen);
        Assert.Equal(5, Assert.Single(p.Uebernommen).Baustoffzuordnungen!["fussbodenaufbau"]);
    }

    [Fact]
    public void Entfernen_nimmt_die_eigene_Zuordnung_zurueck()
    {
        var p = new Protokoll();
        var gemerkt = new Dictionary<string, int> { ["gipsputz"] = 1 };
        var cut = Bauen(p, zuordnen: a => MitBaustoffen(a, gemerkt));
        Einlesen(cut);

        // Eine im Dialog gesetzte Zuordnung fällt beim Entfernen einfach weg.
        Baustoffzeile(cut, "fussbodenaufbau").QuerySelector("select")!.Change("5");
        cut.WaitForAssertion(() => Assert.Equal(2, p.Anfragen.Count));
        Baustoffzeile(cut, "fussbodenaufbau").QuerySelector(".epos-gebimport-entfernen")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(3, p.Anfragen.Count));
        Assert.Empty(p.Anfragen[2].Baustoffzuordnungen!);
        Assert.Contains("epos-gebimport-zeile--gelb", Baustoffzeile(cut, "fussbodenaufbau").ClassName);
        Assert.Contains("6 Aufbauten", cut.Find(".epos-gebimport-bauteile-kopf").TextContent);

        // Eine gemerkte wird zum Entfernen vorgemerkt (null) — gespeichert wird erst mit der Liste.
        IElement gips = Baustoffzeile(cut, "gipsputz");
        Assert.Equal("eigene Zuordnung", Zellen(gips)[2]);
        Assert.Equal("Die eigene Zuordnung von „Gipsputz“ entfernen", gips.QuerySelector(".epos-gebimport-entfernen")!.GetAttribute("title"));
        gips.QuerySelector(".epos-gebimport-entfernen")!.Click();
        cut.WaitForAssertion(() => Assert.Equal(4, p.Anfragen.Count));
        Assert.Equal(new Dictionary<string, int?> { ["gipsputz"] = null }, p.Anfragen[3].Baustoffzuordnungen);
        Assert.Equal("Wortanfang", Zellen(Baustoffzeile(cut, "gipsputz"))[2]);
        Assert.Null(Baustoffzeile(cut, "gipsputz").QuerySelector(".epos-gebimport-entfernen"));

        Ok(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.Equal(new Dictionary<string, int?> { ["gipsputz"] = null }, p.Geschlossen[0]!.Baustoffzuordnungen);
    }

    [Fact]
    public void Ein_anderes_Gebaeude_verwirft_die_Zuordnungen_Abbrechen_liefert_nichts()
    {
        var p = new Protokoll();
        var cut = Bauen(p, lesestand: Gelesen("Haus 1", "Haus 2"), zuordnen: a => MitBaustoffen(a));
        Einlesen(cut);
        Baustoffzeile(cut, "fussbodenaufbau").QuerySelector("select")!.Change("5");
        cut.WaitForAssertion(() => Assert.Equal(2, p.Anfragen.Count));
        Assert.Single(cut.Instance.Baustoffzuordnungen);

        cut.FindAll(".epos-feld").Single(f => f.TextContent.Contains("Gebäude der Datei")).QuerySelector("select")!.Change("1");
        cut.WaitForAssertion(() => Assert.Equal(3, p.Anfragen.Count));
        Assert.Equal(1, p.Anfragen[2].Gebaeudeindex);
        Assert.Empty(p.Anfragen[2].Baustoffzuordnungen!);
        Assert.Empty(cut.Instance.Baustoffzuordnungen);

        Abbrechen(cut);
        cut.WaitForAssertion(() => Assert.Single(p.Geschlossen));
        Assert.Null(p.Geschlossen[0]);
        Assert.Empty(p.Uebernommen);
    }

    [Fact]
    public void Die_Texte_des_Abschnitts_Baustoffe_stehen_auch_englisch()
    {
        GebaeudeImportTexte englisch;
        using (new Kulturvorrichtung("en-US")) englisch = new GebaeudeImportTexte();
        Assert.Equal("Building materials", englisch.GruppeBaustoffe);
        Assert.Equal("Remove assignment", englisch.ZuordnungEntfernen);
        Assert.Equal("Material for “{0}”", englisch.BaustoffWaehlen);
        Assert.Equal("(choose a material)", englisch.BaustoffPlatzhalter);

        var p = new Protokoll();
        var cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, ProfilA);
            c.Add(x => x.Baualtersklassen, Klassen);
            c.Add(x => x.Texte, englisch);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl("C:/ablage/haus.alpha", "haus.alpha", 20555))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)((_, _, _) =>
                Task.FromResult(Gelesen())));
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)(a => { p.Anfragen.Add(a); return MitBaustoffen(a); }));
        });
        cut.FindAll("button").First(k => k.TextContent.Contains(englisch.DateiKnopf.TrimEnd('…', '.'))).Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-baustoffe tbody tr")));

        Assert.Contains(cut.FindAll("h2, h3, .epos-gruppenkopf"), k => k.TextContent.Trim() == "Building materials");
        string kopf = cut.Find(".epos-gebimport-baustoffe thead").TextContent;
        foreach (string spalte in new[] { "Name in the file", "Layers", "Matched by", "Material", "Properties", "Values" })
            Assert.Contains(spalte, kopf);
        Assert.Equal("Material for “Gipsputz”", Baustoffzeile(cut, "gipsputz").QuerySelector("select")!.GetAttribute("aria-label"));
        Assert.Equal("(choose a material)", Baustoffzeile(cut, "fussbodenaufbau").QuerySelector("option[selected]")!.TextContent);
        Baustoffzeile(cut, "fussbodenaufbau").QuerySelector("select")!.Change("5");
        cut.WaitForAssertion(() => Assert.Equal("Remove assignment",
            Baustoffzeile(cut, "fussbodenaufbau").QuerySelector(".epos-gebimport-entfernen")!.TextContent));
    }

    [Fact]
    public void Der_Klassenwechsel_ordnet_neu_zu_und_behaelt_die_Handaenderungen()
    {
        var p = new Protokoll();
        var cut = Bauen(p);
        Einlesen(cut);
        Assert.Single(p.Anfragen);

        ZeileVon(cut, "NUTZ").QuerySelector("input.epos-eingabe")!.Input("77");
        // Eine Handänderung ordnet neu zu — mit dem Handwert, damit die Datenseite nachzieht.
        cut.WaitForAssertion(() => Assert.Equal(2, p.Anfragen.Count));
        Assert.Equal(77, p.Anfragen[1].Handwerte!["NUTZ"]);
        ZeileVon(cut, "FNORD").QuerySelector("input[type=checkbox]")!.Change(false);
        cut.FindAll(".epos-feld")[0].QuerySelector("select")!.Change("4");   // Klasse E

        cut.WaitForAssertion(() => Assert.Equal(3, p.Anfragen.Count));
        Assert.Equal(4, p.Anfragen[2].Baualtersklasse);
        Assert.Equal(77, p.Anfragen[2].Handwerte!["NUTZ"]);
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

    // =====================================================================
    //  Nacharbeit G4b: der Azimut der Bauteilliste, gefahren mit der echten Hülle
    // =====================================================================

    /// <summary>
    /// Die Bauteilliste zeigt den Azimut mit höchstens einer Nachkommastelle. Gefahren mit der echten Hülle
    /// (ohne Projekt, ohne Datenbank) und dem IFC-Probenhaus, dessen Lageplan gegen Nord gedreht ist — seine
    /// Azimute tragen mehr Stellen. Das Bauteil des Vorschlags behält den ungerundeten Wert der Datei.
    /// </summary>
    [Fact]
    public void Die_Bauteilliste_zeigt_den_Azimut_auf_eine_Nachkommastelle_gerundet()
    {
        string probe = Path.Combine(Wurzel(), "Referenzlaeufe", "Importproben", "ifc4_haus.ifc");
        var huelle = new WindowsFormsApplication1.GebaeudeImportHuelle();
        IReadOnlyDictionary<string, object> g = huelle.Gaben();
        IRenderedComponent<GebaeudeImportDialog> cut = Render<GebaeudeImportDialog>(c =>
        {
            c.Add(x => x.Profil, g["Profil"] as GebaeudeImportProfilDaten);
            c.Add(x => x.Baualtersklassen, (IReadOnlyList<string>)g["Baualtersklassen"]);
            c.Add(x => x.DateiWaehlen, (Func<string, Task<GebaeudeDateiwahl?>>)(_ =>
                Task.FromResult<GebaeudeDateiwahl?>(new GebaeudeDateiwahl(probe, "ifc4_haus.ifc", new FileInfo(probe).Length))));
            c.Add(x => x.Lesen, (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)g["Lesen"]);
            c.Add(x => x.Zuordnen, (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)g["Zuordnen"]);
            c.Add(x => x.Pruefen, (Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>)g["Pruefen"]);
        });
        Einlesen(cut);
        // Die Bauteilliste ist die des Einzonenwegs: die Regel „eine Zone je Gebäude" (Vorgabe ist je Geschoss).
        IElement regel = cut.FindAll("select").First(s => s.TextContent.Contains("Z5 –"));
        regel.Change(regel.QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Z5", StringComparison.Ordinal)).GetAttribute("value"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".epos-gebimport-bauteilliste tbody tr")));

        List<string> angezeigt = cut.FindAll(".epos-gebimport-bauteilliste tbody tr")
                                    .Select(tr => tr.Children[4].TextContent.Trim()).ToList();
        Assert.All(angezeigt, a => Assert.Matches(@"^(—|\d{1,3}(,\d)?°)$", a));
        // Der gedrehte Lageplan gibt Azimute mit Nachkommastellen (den Wert des Bauteils hält GebaeudeImportHuelleTests).
        Assert.Contains(angezeigt, a => a.Contains(','));
    }

    [Theory]
    [InlineData(63.43494882, "63,4°")]
    [InlineData(243.468, "243,5°")]
    [InlineData(90.0, "90°")]
    [InlineData(359.96, "0°")]
    [InlineData(-0.04, "0°")]
    public void Der_Azimuttext_rundet_auf_eine_Stelle(double azimut, string text)
        => Assert.Equal(text, WindowsFormsApplication1.GebaeudeImportHuelle.AzimutText(azimut));

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;
        Assert.NotNull(d);
        return d!.FullName;
    }
}
