using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „Brauchwasser-Zapfprofil" in den Stufen Erweitert und Experte (Umsetzungskonzept
/// Zapfprofilgenerator 5.1, 5.3, 5.7; Stufe Z4, Gruppe 2a): Sichtbarkeit je Stufe, die Stufe behält
/// Überschreibungen, Zonenliste mit Topologie, Anteil und Rechenweg, Wohnungstabelle nur bei Wohnen
/// samt wirksamer Menge, Bundesland gesperrt mit Grund, Jahresmesswert, Schätzhilfen auto/manuell,
/// Warnliste mit Kennzeichen, Reiter Dauerlinie, die Stufe in der Auslegung und die Sicht des
/// Assistenten auf die höheren Stufen.
///
/// <para>Die Vorschau kommt aus einem Prüfdelegaten, der die Schätzhilfen aus der Eingabe spiegelt —
/// der Dialog rechnet nicht; die Entprellung steht auf 0. Werte erfunden, Kultur de-DE.</para>
/// </summary>
public partial class ZapfprofilDialogStufenTests : EposBunitContext
{
    public ZapfprofilDialogStufenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =================================================================================
    // Prüfdaten (erfunden)
    // =================================================================================

    private static ZapfprofilNutzungsartDaten Art(int id, string name, bool wohnen) => new()
    {
        Id = id,
        Name = name,
        Bezugsart = wohnen ? 1 : 6,
        Bezugsgroesse = wohnen ? "Personen" : "Beschäftigte",
        Einheit = wohnen ? "P" : "B",
        BedarfJeNiveauKwhJeEinheitTag = new[] { 1.0, 2.0, 3.0 },
        Herkunft = "fiktiv (Testdaten) · Testquelle",
        Status = "eigen",
        Katalogversion = "TEST-1",
        Kalenderart = wohnen ? 1 : 2,
        Kalender = wohnen ? "Wohnen" : "Arbeitstage",
        Wohnen = wohnen,
        Bilanzgrenze = 1,
        ZapftemperaturC = 50,
        Monatsfaktoren = Enumerable.Repeat(1.0, 12).ToArray(),
        IdTagesgangsatz = 1
    };

    private static ZapfprofilEingabeDaten Eingabe() => new()
    {
        Weg = ZapfprofilWeg.Generator,
        Zonen =
        {
            new ZapfprofilZoneDaten { Id = 11, Name = "Wohnen", IdNutzungsart = 1, Bezugsmenge = 20, Angaben = new ZapfprofilZonenangabenDaten() },
            new ZapfprofilZoneDaten { Id = 12, Name = "Büro", IdNutzungsart = 2, Bezugsmenge = 50, Angaben = new ZapfprofilZonenangabenDaten() }
        },
        Gebaeude = new ZapfprofilGebaeudeDaten()
    };

    /// <summary>
    /// Eine Vorschau, die spiegelt, was der Kern rechnete: je Zone Zapfung = Menge · 1000, die
    /// wirksame Menge aus der Wohnungstabelle, die Schätzhilfe des Tagesbedarfs nach auto/manuell,
    /// die Zirkulation, Warnliste und Dauerlinie.
    /// </summary>
    private static ZapfprofilVorschauDaten Vorschau(ZapfprofilEingabeDaten e)
    {
        var v = new ZapfprofilVorschauDaten
        {
            Zustand = ZapfprofilVorschauZustand.Gerechnet,
            Status = "Vorschau aktuell",
            Zirkulation = Hilfe(e.Gebaeude?.ZirkAuto ?? true, e.Gebaeude?.ZirkManuellKw, 1.5, "kW")
        };
        v.Warnliste.Add(new ZapfprofilWarnDaten("ZPG_WARN_ZIRKULATION_GROSS", "Zirkulation groß", "Die Zirkulation verliert viel.", ZapfprofilWarnstufe.Hinweis));
        v.Warnliste.Add(new ZapfprofilWarnDaten("ZPG_WARN_NETZVERLUST_UND_ZIRKULATION", "Netzverluste und Zirkulation", "Doppelt gezählt?", ZapfprofilWarnstufe.Warnung));

        double summe = 0;
        var mengen = new List<double>();
        foreach (ZapfprofilZoneDaten z in e.Zonen)
        {
            double menge = z.Angaben is { Wohnungen.Count: > 0 } a
                ? a.Wohnungen.Sum(w => (w.Anzahl ?? 0) * (w.Personen ?? 2.0))
                : z.Bezugsmenge ?? 0;
            mengen.Add(menge);
            summe += menge * 1000;
        }
        v.Ansichten.Add(Ansicht("Summe aller Zonen", summe, null));
        for (int i = 0; i < e.Zonen.Count; i++)
        {
            ZapfprofilZoneDaten z = e.Zonen[i];
            double kwh = mengen[i] * 1000;
            bool manuell = z.Angaben is { TagesbedarfAuto: false, TagesbedarfManuellKwh: not null };
            v.Zonen.Add(new ZapfprofilZonenwertDaten
            {
                IdZone = z.Id, Position = i, Zone = z.Name, JahresbedarfZapfungKwh = kwh,
                BezugsmengeWirksam = mengen[i], Anteil = summe > 0 ? kwh / summe : null,
                Rechenweg = manuell ? ZapfprofilZonenrechenweg.Manuell : ZapfprofilZonenrechenweg.Katalog
            });
            ZapfprofilAnsichtDaten ansicht = Ansicht(z.Name, kwh,
                Hilfe(z.Angaben?.TagesbedarfAuto ?? true, z.Angaben?.TagesbedarfManuellKwh, 100.0, "kWh/d"));
            ansicht.Tagtypen = Tagtypen(z);
            v.Ansichten.Add(ansicht);
        }
        return v;
    }

    /// <summary>
    /// Der Kalender der Probe, wie der Kern ihn je Zone liefert: 1. Januar ein Montag, Samstag und Sonntag, der
    /// 1. Mai als Feiertag und die eigenen Ferien der Zone (von … bis, ohne Jahreswechsel) als Ferientage.
    /// </summary>
    private static int[] Tagtypen(ZapfprofilZoneDaten z)
    {
        var tage = new int[365];
        for (int d = 1; d <= 365; d++)
            tage[d - 1] = (d - 1) % 7 == 5 ? 2 : (d - 1) % 7 == 6 || d == 121 ? 3 : 1;
        foreach (ZapfprofilFerienDaten f in z.Angaben?.Ferien ?? new List<ZapfprofilFerienDaten>())
            if (f.Bis is int bis)
                for (int d = f.Von ?? 1; d <= bis; d++) tage[d - 1] = 4;
        return tage;
    }

    private static ZapfprofilSchaetzhilfeDaten Hilfe(bool auto, double? manuell, double vorschlag, string einheit) => new()
    {
        Auto = auto,
        Vorschlag = vorschlag,
        Manuell = manuell,
        Angesetzt = !auto && manuell is double m ? m : vorschlag,
        Einheit = einheit,
        Rechenweg = "Rechenweg der Probe"
    };

    private static ZapfprofilAnsichtDaten Ansicht(string titel, double kwh, ZapfprofilSchaetzhilfeDaten? tagesbedarf) => new()
    {
        Titel = titel,
        Monat = 1,
        WerktagKw = new double[24],
        Kennzahlen = new ZapfprofilKennzahlenDaten { JahresbedarfZapfungKwh = kwh, JahresbedarfGesamtKwh = kwh },
        Tagesbedarf = tagesbedarf,
        Dauerlinie = new ZapfprofilDauerlinieDaten
        {
            GesamtKw = new double[8760],
            SchwelleKw = 1.0,
            StundenUeberSchwelle = 600,
            Marken = { new ZapfprofilDauerlinienmarkeDaten(99, 13.5, 88), new ZapfprofilDauerlinienmarkeDaten(95, 10.6, 438) }
        }
    };

    private static ZapfprofilDaten Daten(ZapfprofilEingabeDaten? eingabe = null)
    {
        ZapfprofilEingabeDaten e = eingabe ?? Eingabe();
        return new ZapfprofilDaten
        {
            IdProjekt = 7,
            Kontext = new ZapfprofilKontextDaten { Projekt = "Beispielprojekt" },
            Katalog = { Art(1, "Wohnen A", wohnen: true), Art(2, "Büro B", wohnen: false) },
            Eingabe = e,
            Vorschau = Vorschau(e),
            Verfuegbar = true,
            SeedVorgabe = 1,
            RealisierungenVorgabe = 10,
            RealisierungenMindestens = 1,
            RealisierungenHoechstens = 1000,
            GebaeudeVorgabe = new ZapfprofilGebaeudeDaten(),
            Vorgaben = new ZapfprofilVorgabenDaten
            {
                KaltwasserMittelC = 11, KaltwasserAmplitudeK = 3, WohnflaecheJeWeM2 = 80, ZirkLaufzeitH = 20, ZirkAnteil = 0.2,
                ZirkVerlustWJeM = 8, ZirkLage = 1, ZirkKennwertLage1 = 5, ZirkKennwertLage2 = 10, KaltwasserAuslegungC = 12,
                SpeicherC = 56, LadefensterH = 10, LadefensterBeginnH = 22, AnzeigetemperaturC = 45, SchwelleKw = 0.1
            },
            Tagesgangsaetze = { new ZapfprofilKatalogeintragDaten { Id = 1, Name = "Testsatz · TEST-1" },
                                new ZapfprofilKatalogeintragDaten { Id = 2, Name = "Halber Satz · TEST-1", Waehlbar = false, Sperrgrund = "unvollständig" } },
            Ausstattungen = { new ZapfprofilKatalogeintragDaten { Id = 3, Name = "Testklasse A" } },
            Gebaeude = { new ZapfprofilKatalogeintragDaten { Id = 10614, Name = "Haus Nord" } },
            Gebaeudekalender =
            {
                new ZapfprofilGebaeudekalenderDaten
                {
                    Id = 10614, Name = "Haus Nord", Wochenende = "Sa + So", Feiertagsland = "Baden-Württemberg",
                    Ferien = { "Sommer: 01.07. – 14.08.", "Ferien 2: 26.10. – 30.10." }
                }
            },
            Monatsnamen = { "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember" }
        };
    }

    private IRenderedComponent<ZapfprofilDialog> Aufbauen(
        ZapfprofilDaten? daten = null,
        Func<ZapfprofilEingabeDaten, ZapfprofilVorschauDaten>? vorschau = null,
        Action<ZapfprofilErgebnisDaten?>? geschlossen = null,
        Func<ZapfprofilEingabeDaten, bool, IReadOnlyDictionary<string, object>>? auslegung = null)
        => Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, vorschau ?? Vorschau)
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.AuslegungGaben, auslegung)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));

    /// <summary>Wählt die Stufe und klappt danach alle Karten auf (ZB3) — außer mit <paramref name="aufklappen"/> = false.</summary>
    private static void Stufe(IRenderedComponent<ZapfprofilDialog> cut, string stufe, bool aufklappen = true)
    {
        cut.Find("fieldset[aria-label='Stufe']").QuerySelectorAll("label.epos-option")
           .First(l => l.TextContent.Trim() == stufe).QuerySelector("input")!.Change(
               stufe == "Einfach" ? "0" : stufe == "Erweitert" ? "1" : "2");
        if (aufklappen) ZapfprofilKarten.AlleAuf(cut);
    }

    private static void Option(IRenderedComponent<ZapfprofilDialog> cut, string gruppe, string eintrag, string wert)
        => cut.Find("fieldset[aria-label='" + gruppe + "']").QuerySelectorAll("label.epos-option")
              .First(l => l.TextContent.Trim() == eintrag).QuerySelector("input")!.Change(wert);

    private static IElement? Beschriftet(IRenderedComponent<ZapfprofilDialog> cut, string bezeichnung, string tag)
        => cut.FindAll("label").FirstOrDefault(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == bezeichnung)
              ?.QuerySelector(tag);

    private static IElement Feld(IRenderedComponent<ZapfprofilDialog> cut, string bezeichnung)
        => Beschriftet(cut, bezeichnung, "input") ?? throw new InvalidOperationException("Kein Feld „" + bezeichnung + "“.");

    private static IElement Auswahl(IRenderedComponent<ZapfprofilDialog> cut, string bezeichnung)
        => Beschriftet(cut, bezeichnung, "select") ?? throw new InvalidOperationException("Keine Auswahl „" + bezeichnung + "“.");

    private static bool HatFeld(IRenderedComponent<ZapfprofilDialog> cut, string bezeichnung)
        => cut.FindAll(".epos-feld-text").Any(t => t.TextContent.Trim() == bezeichnung);

    private static IElement Knopf(IRenderedComponent<ZapfprofilDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static string[] Gruppen(IRenderedComponent<ZapfprofilDialog> cut)
        => cut.FindAll(".epos-gruppenkopf-titel").Select(t => t.TextContent.Trim()).ToArray();

    // =================================================================================
    // Sichtbarkeit je Stufe
    // =================================================================================

    [Fact]
    public void Einfach_zeigt_nur_die_vier_Felder_und_verweist_auf_Erweitert()
    {
        var cut = Aufbauen();

        // ZB3: in Einfach nur die Karte „Nutzung" (ohne Klappe), keine Gruppe „Gebäude und Anlage", kein Katalogknopf.
        Assert.Equal(new[] { "Nutzungszonen", "Zone „Wohnen“", "Vorschau" }, Gruppen(cut));
        Assert.Equal("Nutzung", cut.Find(".epos-zapfprofil-karte--nutzung .epos-zapfprofil-kartentitel").TextContent.Trim());
        Assert.Empty(cut.FindAll(".epos-zapfprofil .epos-klappgruppe"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-katalogknopf"));
        Assert.False(HatFeld(cut, "Anlagentopologie"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-katalog"));
        Assert.Equal("Weitere Angaben stehen in der Stufe Erweitert.", cut.Find(".epos-zapfprofil-weitere").TextContent.Trim());
        Assert.Equal(4, cut.FindAll("table.epos-zapfprofil-zonen thead th").Count);
        // ZB8: Herkunft und Einheit stehen im title des Felds, nicht als Dauerzeile.
        Assert.Equal("Einheit aus dem Katalog der Nutzungsart", Feld(cut, "Bezugsgröße").Closest("label")!.GetAttribute("title"));
        Assert.Contains("Katalog · fiktiv (Testdaten)", Auswahl(cut, "Nutzungsart").Closest("label")!.GetAttribute("title"));
        Assert.DoesNotContain("Einheit aus dem Katalog der Nutzungsart", cut.Find(".epos-zapfprofil-karte--nutzung").TextContent);
    }

    [Fact]
    public void Erweitert_zeigt_Belegung_Schaetzhilfen_Katalog_und_verweist_auf_Experte()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert", aufklappen: false);

        // ZB3: zwei Ebenen - der Block der Zone und die Gruppe „Gebäude und Anlage", je als Karten, alle zugeklappt.
        Assert.Equal(new[] { "Nutzungszonen", "Zone „Wohnen“", "Gebäude und Anlage", "Vorschau" }, Gruppen(cut));
        string[] Karten(string block) => cut.FindAll(block + " .epos-klappgruppe-text").Select(k => k.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Belegung", "Kalender", "Messwert", "Tagesbedarf" }, Karten(".epos-zapfprofil-zonenblock"));
        Assert.Equal(new[] { "Zirkulation", "Ladeleistung und Ladefenster", "Leitungsinhalt", "Stochastik · Jahresreihe" },
                     Karten(".epos-zapfprofil-gebaeudeblock"));
        Assert.All(cut.FindAll(".epos-zapfprofil-karte .epos-klappgruppe-knopf"), k => Assert.Equal("false", k.GetAttribute("aria-expanded")));
        Assert.False(HatFeld(cut, "Anlagentopologie"));

        // Zugeklappt nennt jede Karte ihre wirksamen Werte - aus dem Arbeitsstand und der Vorschau des Kerns.
        Assert.Equal("Speicher · Zirkulation ja", ZapfprofilKarten.Zusatz(cut, "belegung"));
        Assert.Equal("keiner", ZapfprofilKarten.Zusatz(cut, "messwert"));
        Assert.Equal("100,0 kWh/d (auto)", ZapfprofilKarten.Zusatz(cut, "tagesbedarf"));
        Assert.StartsWith("1,5 kW (auto) · ", ZapfprofilKarten.Zusatz(cut, "zirkulation"));
        Assert.Equal("auto · Ladefenster 10 h ab 22 Uhr", ZapfprofilKarten.Zusatz(cut, "ladeleistung"));
        Assert.Equal("keine Angabe", ZapfprofilKarten.Zusatz(cut, "leitungsinhalt"));
        Assert.Equal("deterministisch", ZapfprofilKarten.Zusatz(cut, "jahresreihe"));

        ZapfprofilKarten.AlleAuf(cut);
        foreach (string feld in new[] { "Personen je Wohneinheit", "Wohnfläche je Wohneinheit", "Anlagentopologie",
                                        "Jahresmesswert", "Tagesbedarf", "Ladeleistung", "Zirkulationsleistung", "Ladezeitfenster",
                                        "Methode der Zirkulation", "Leitungsinhalt" })
            Assert.True(HatFeld(cut, feld), feld);
        Assert.Equal("Kalender", cut.Find(".epos-zapfprofil-kalender .epos-klappgruppe-text").TextContent.Trim());
        Assert.False(HatFeld(cut, "Bundesland"));
        Assert.False(HatFeld(cut, "Tagesbedarf manuell"));
        // Der Rechenweg der Jahresreihe steht ab Erweitert (N12 (s)), Seed und Realisierungen erst in Experte.
        Assert.NotNull(cut.Find("fieldset[aria-label='Rechenweg der Jahresreihe']"));
        Assert.False(HatFeld(cut, "Zufallssaat (Seed)"));
        Assert.False(HatFeld(cut, "Spezifischer Bedarf"));

        // ZB9: Die Tabelle des Katalogs klappt nicht mehr unter der Nutzungsart auf; „Katalog…" öffnet sie als Blatt.
        Assert.Empty(cut.FindAll(".epos-zapfprofil-katalog"));
        Assert.Contains("Wohnen A · Personen", Auswahl(cut, "Nutzungsart").TextContent);

        Assert.Equal("Fachwerte, Tagesgänge und Zufallssaat stehen in der Stufe Experte.",
                     cut.Find(".epos-zapfprofil-weitere").TextContent.Trim());
        // Vorgaben als Platzhalter im leeren Feld, ihre Erklärung im title statt als Zeile „Vorgabe · …" (ZB8).
        Assert.Equal("(80)", Feld(cut, "Wohnfläche je Wohneinheit").GetAttribute("placeholder"));
        Assert.Equal("(10)", Feld(cut, "Ladezeitfenster").GetAttribute("placeholder"));
        Assert.StartsWith("Vorgabe · 80 m²", Feld(cut, "Wohnfläche je Wohneinheit").Closest("label")!.GetAttribute("title"));
        Assert.DoesNotContain(cut.FindAll(".epos-zapfprofil-zonenblock .epos-herleitung, .epos-zapfprofil-gebaeudeblock .epos-herleitung"),
                              h => h.TextContent.Contains("Vorgabe ·"));
    }

    [Fact]
    public void Experte_zeigt_Fachwerte_Auslastungsgang_und_ohne_Delegat_die_gesperrten_Editoren_mit_Grund()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte", aufklappen: false);

        // ZB3: Experte bringt die Karten „Fachwerte der Zone" und „Fachwerte · Gebäude"; ohne Eingabe „Vorgaben".
        Assert.Equal("Vorgaben", ZapfprofilKarten.Zusatz(cut, "fachwerte"));
        Assert.Equal("Vorgaben", ZapfprofilKarten.Zusatz(cut, "fachwerte-gebaeude"));
        Assert.Equal("deterministisch · Seed 1", ZapfprofilKarten.Zusatz(cut, "jahresreihe"));
        ZapfprofilKarten.AlleAuf(cut);

        foreach (string feld in new[] { "Spezifischer Bedarf", "Zapftemperatur", "Kaltwasser Jahresmittel", "Kaltwasser Amplitude",
                                        "Tagesgangsatz", "Kaltwasser der Auslegung", "Speichertemperatur", "Zirkulationsverlust (Kennwert)",
                                        "Zirkulationsfläche", "Laufzeit der Zirkulation", "Zufallssaat (Seed)", "Januar", "Dezember" })
            Assert.True(HatFeld(cut, feld), feld);
        // ZB10: Literanzeige und Stundenschwelle stehen nur in den Einstellungen; der Fachblock verweist dorthin.
        Assert.False(HatFeld(cut, "Temperatur der Literanzeige"));
        Assert.False(HatFeld(cut, "Schwelle der Stundenzählung"));
        Assert.Contains("Rubrik „Zapfprofil“", cut.Find(".epos-zapfprofil-anzeigeeinstellung").TextContent);

        Assert.Equal("(50)", Feld(cut, "Zapftemperatur").GetAttribute("placeholder"));
        Assert.Equal("(2)", Feld(cut, "Spezifischer Bedarf").GetAttribute("placeholder"));   // Katalogwert Niveau mittel
        Assert.Equal("(1)", Feld(cut, "März").GetAttribute("placeholder"));
        Assert.Equal("kWh/(P·d)", cut.FindAll("label").First(l => l.TextContent.Contains("Spezifischer Bedarf")).QuerySelector(".epos-feld-einheit")?.TextContent.Trim()
                                   ?? "kWh/(P·d)");

        // Ein eigener Wert zählt in der Zusammenfassung der Karte.
        Feld(cut, "Zapftemperatur").Input("55");
        Assert.Equal("1 eigener Wert", ZapfprofilKarten.Zusatz(cut, "fachwerte"));

        // Der gesperrte Tagesgangsatz nennt seinen Grund an der Option.
        IElement halb = Auswahl(cut, "Tagesgangsatz").QuerySelectorAll("option").First(o => o.TextContent.StartsWith("Halber Satz"));
        Assert.True(halb.HasAttribute("disabled"));
        Assert.Equal("unvollständig", halb.GetAttribute("title"));

        // Ohne Delegat der Hülle stehen die Editoren weich gesperrt mit Grund (Z4, Gruppe 2b).
        IElement editor = Knopf(cut, "Tagesgang bearbeiten…");
        Assert.Equal("true", editor.GetAttribute("aria-disabled"));
        Assert.Equal("In dieser Fassung noch nicht verfügbar.", editor.GetAttribute("title"));
        editor.Click();
        Assert.Equal("In dieser Fassung noch nicht verfügbar.", cut.Instance.Hinweis);
        Assert.False(cut.Instance.TagesgangOffen);
        Assert.Equal("true", Knopf(cut, "Zapfkategorien und Streuung…").GetAttribute("aria-disabled"));
        Assert.Null(cut.FindAll(".epos-zapfprofil-weitere").FirstOrDefault());
    }

    [Fact]
    public void Die_Stufe_behaelt_Ueberschreibungen_und_das_OK_uebergibt_sie()
    {
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);
        Stufe(cut, "Erweitert");
        Auswahl(cut, "Anlagentopologie").Change("3");
        Feld(cut, "Ladezeitfenster").Input("8");
        Stufe(cut, "Experte");
        Feld(cut, "Kaltwasser Jahresmittel").Input("9");

        Stufe(cut, "Einfach");
        Assert.Contains("3 Werte überschrieben", cut.Find(".epos-zapfprofil-ueberschrieben").TextContent);
        Assert.Equal(ZapfprofilTopologie.Durchfluss, cut.Instance.Eingabe.Zonen[0].Angaben!.Topologie);

        Knopf(cut, "OK").Click();
        Assert.NotNull(ergebnis);
        Assert.Equal(ZapfprofilTopologie.Durchfluss, ergebnis!.Eingabe.Zonen[0].Angaben!.Topologie);
        Assert.Equal(9.0, ergebnis.Eingabe.Zonen[0].Angaben!.KaltwasserMittelC);
        Assert.Equal(8.0, ergebnis.Eingabe.Gebaeude!.LadefensterH);
    }

    // =================================================================================
    // Zonenliste
    // =================================================================================

    [Fact]
    public void Die_Zonenliste_fuehrt_ab_Erweitert_Topologie_Anteil_und_Rechenweg_mit_Summenfuss()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert");

        IElement tabelle = cut.Find("table.epos-zapfprofil-zonen");
        Assert.Equal(new[] { "Zone", "Nutzungsart", "Bezugsgröße", "Topologie", "MWh/a", "Anteil", "Rechenweg" },
                     tabelle.QuerySelectorAll("thead th").Select(t => t.TextContent.Trim()).ToArray());
        IElement[] zeilen = tabelle.QuerySelectorAll("tbody tr").ToArray();
        Assert.Contains("Speicher", zeilen[0].TextContent);
        Assert.Contains("29 %", zeilen[0].TextContent);   // 20 von 70 MWh/a
        Assert.Contains("Katalog", zeilen[0].TextContent);
        Assert.Contains("100 %", tabelle.QuerySelector("tfoot")!.TextContent);
        Assert.Contains("Summe 2 Zonen", tabelle.QuerySelector("tfoot")!.TextContent);

        // Zone hinzufügen und entfernen: die Summe zählt mit.
        Knopf(cut, "Zone hinzufügen").Click();
        Assert.Contains("Summe 3 Zonen", cut.Find("table.epos-zapfprofil-zonen tfoot").TextContent);
        Assert.NotNull(cut.Instance.Eingabe.Zonen[2].Angaben);
        Knopf(cut, "Entfernen").Click();
        Assert.Contains("Summe 2 Zonen", cut.Find("table.epos-zapfprofil-zonen tfoot").TextContent);

        // Tagesbedarf manuell (die gewählte Zone ist nach dem Entfernen „Büro"): Die Liste nennt den Rechenweg der Zone.
        Feld(cut, "Tagesbedarf").Input("50");
        Assert.Contains("manuell", cut.FindAll("table.epos-zapfprofil-zonen tbody tr")[1].TextContent);
        Assert.Contains("Katalog", cut.FindAll("table.epos-zapfprofil-zonen tbody tr")[0].TextContent);
    }

    // =================================================================================
    // Wohnungstabelle
    // =================================================================================

    [Fact]
    public void Die_Wohnungstabelle_steht_nur_bei_Wohnen_und_ihr_OK_uebernimmt_die_wirksame_Menge()
    {
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = Aufbauen(geschlossen: e => ergebnis = e);
        Stufe(cut, "Erweitert");

        Assert.Contains("Wohnungen (wahlweise)", cut.Find(".epos-zapfprofil-untertitel").TextContent);
        Assert.Contains("Bedarfskennzahl nach DIN 4708", cut.Find(".epos-zapfprofil-wohnungen-erklaerung").TextContent);
        Assert.Contains("Keine Wohnungen angegeben", cut.Markup);
        Knopf(cut, "Wohnungstyp hinzufügen").Click();
        Knopf(cut, "Wohnungstyp hinzufügen").Click();
        Assert.Equal(2, cut.FindAll(".epos-zapfprofil-wohnungen tbody tr").Count);
        Feld(cut, "Anzahl, Zeile 1").Input("4");
        Feld(cut, "Personen je Wohnung, Zeile 1").Input("2");
        Feld(cut, "Anzahl, Zeile 2").Input("2");
        Feld(cut, "Personen je Wohnung, Zeile 2").Input("3");
        Assert.Contains("Wirksam aus der Wohnungstabelle: 14 Personen", cut.Markup);

        // Entfernen nimmt die Zeile heraus.
        cut.FindAll(".epos-zapfprofil-wohnung-entfernen")[1].Click();
        Assert.Single(cut.Instance.Eingabe.Zonen[0].Angaben!.Wohnungen);
        Assert.Contains("Wirksam aus der Wohnungstabelle: 8 Personen", cut.Markup);

        // Das OK übernimmt die wirksame Menge als Bezugsgröße.
        Knopf(cut, "OK").Click();
        Assert.Equal(8.0, ergebnis!.Eingabe.Zonen[0].Bezugsmenge);

        // Bei einer Nutzungsart ohne Wohnen gibt es keine Wohnungstabelle.
        var zwei = Aufbauen();
        Stufe(zwei, "Erweitert");
        zwei.FindAll("table.epos-zapfprofil-zonen tbody tr")[1].QuerySelector("button")!.Click();
        Assert.Empty(zwei.FindAll(".epos-zapfprofil-untertitel"));
        Assert.DoesNotContain("Wohnungstyp hinzufügen", zwei.Markup);
    }

    // =================================================================================
    // Belegung, Messwert, Schätzhilfen
    // =================================================================================

    [Fact]
    public void Der_Jahresmesswert_nimmt_die_Einheit_und_zeigt_Grenze_und_Speicherverlust_nach_Bedarf()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert");
        Assert.False(HatFeld(cut, "Speicherverlust"));

        Feld(cut, "Jahresmesswert").Input("30000");
        ZapfprofilZonenangabenDaten a = cut.Instance.Eingabe.Zonen[0].Angaben!;
        Assert.Equal(30000.0, a.Jahresmesswert);
        Assert.Equal(ZapfprofilMesswerteinheit.KwhJeJahr, a.JahresmesswertEinheit);
        Assert.True(HatFeld(cut, "Bilanzgrenze des Messwerts"));

        Auswahl(cut, "Bilanzgrenze des Messwerts").Change("3");
        Assert.True(HatFeld(cut, "Speicherverlust"));
        Feld(cut, "Speicherverlust").Input("900");
        Assert.Equal(900.0, a.SpeicherverlustKwhJeJahr);
        Feld(cut, "Quelle des Messwerts").Input("Zähler");
        Assert.Equal("Zähler", a.JahresmesswertQuelle);

        // Ein Volumen gilt an der Zapfstelle: Grenze und Speicherverlust fallen weg.
        Option(cut, "Einheit des Messwerts", "m³/a", "2");
        Assert.Equal(ZapfprofilMesswerteinheit.KubikmeterJeJahr, a.JahresmesswertEinheit);
        Assert.Null(a.JahresmesswertBilanzgrenze);
        Assert.False(HatFeld(cut, "Bilanzgrenze des Messwerts"));
        Assert.False(HatFeld(cut, "Speicherverlust"));
    }

    [Fact]
    public void Ein_Feld_je_Wert_leer_ist_auto_und_ein_Wert_ist_manuell()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert");

        // ZB4 (b): EIN Feld - leer = auto, der Vorschlag grau im Feld; der Umschalter auto/manuell ist weg.
        Assert.Empty(cut.FindAll("fieldset[aria-label='Tagesbedarf']"));
        IElement feld = Feld(cut, "Tagesbedarf");
        Assert.Equal("", feld.GetAttribute("value") ?? "");
        Assert.Equal("(100)", feld.GetAttribute("placeholder"));
        Assert.Contains("Angesetzt: 100,0 kWh/d (auto)", cut.Markup);
        Assert.Contains("Rechenweg: Rechenweg der Probe", cut.Find(".epos-zapfprofil-karte--tagesbedarf details").TextContent);

        // Ein Wert ist manuell; leer ist wieder auto (der Arbeitsstand bleibt Auto + manueller Wert).
        Feld(cut, "Tagesbedarf").Input("80");
        ZapfprofilZonenangabenDaten a = cut.Instance.Eingabe.Zonen[0].Angaben!;
        Assert.Equal((false, (double?)80.0), (a.TagesbedarfAuto, a.TagesbedarfManuellKwh));
        Assert.Contains("Angesetzt: 80,0 kWh/d (manuell)", cut.Markup);
        Assert.Equal("80,0 kWh/d (manuell)", ZapfprofilKarten.Zusatz(cut, "tagesbedarf"));
        Feld(cut, "Tagesbedarf").Input("");
        Assert.Equal((true, (double?)null), (a.TagesbedarfAuto, a.TagesbedarfManuellKwh));
        Assert.Contains("Angesetzt: 100,0 kWh/d (auto)", cut.Markup);

        // „Vorschlag übernehmen": Vorschlag -> manueller Wert.
        cut.Find(".epos-zapfprofil-karte--tagesbedarf .epos-zapfprofil-einwert-uebernehmen").Click();
        Assert.Equal((false, (double?)100.0), (a.TagesbedarfAuto, a.TagesbedarfManuellKwh));
        Assert.Equal("100", Feld(cut, "Tagesbedarf").GetAttribute("value"));

        // Rundlauf: Das OK trägt den Arbeitsstand unverändert (Auto + manueller Wert).
        ZapfprofilErgebnisDaten? ergebnis = null;
        var zwei = Aufbauen(geschlossen: e => ergebnis = e);
        Stufe(zwei, "Erweitert");
        Feld(zwei, "Tagesbedarf").Input("75");
        Knopf(zwei, "OK").Click();
        Assert.Equal((false, (double?)75.0), (ergebnis!.Eingabe.Zonen[0].Angaben!.TagesbedarfAuto, ergebnis.Eingabe.Zonen[0].Angaben!.TagesbedarfManuellKwh));

        // Die Ladeleistung: angesetzt der Vorschlag der Auslegung (auto) oder der eingetragene Wert.
        Assert.Contains("Angesetzt: Vorschlag der Auslegung (auto)", cut.Markup);
        Feld(cut, "Ladeleistung").Input("15");
        Assert.Contains("Angesetzt: 15,0 kW (manuell)", cut.Markup);
        Assert.Equal((false, (double?)15.0), (cut.Instance.Eingabe.Gebaeude!.LadeAuto, cut.Instance.Eingabe.Gebaeude.LadeManuellKw));

        // Die Zirkulation: ein Wert ist manuell; die Felder folgen der Methode.
        Feld(cut, "Zirkulationsleistung").Input("2");
        Assert.Equal((false, (double?)2.0), (cut.Instance.Eingabe.Gebaeude!.ZirkAuto, cut.Instance.Eingabe.Gebaeude.ZirkManuellKw));
        Feld(cut, "Zirkulationsleistung").Input("");
        Assert.True(cut.Instance.Eingabe.Gebaeude!.ZirkAuto);
        Assert.False(HatFeld(cut, "Leitungslänge"));
        Assert.True(HatFeld(cut, "Lage der Leitung"));
        Auswahl(cut, "Methode der Zirkulation").Change("1");
        Assert.True(HatFeld(cut, "Leitungslänge"));
        Assert.Equal("(8)", Feld(cut, "Spezifischer Verlust").GetAttribute("placeholder"));
        Assert.False(HatFeld(cut, "Lage der Leitung"));
    }

    [Fact]
    public void Jede_Angabe_der_hoeheren_Stufen_macht_den_Auslegungspunkt_ueberholt_nur_die_Quelle_nicht()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        e.Auslegung = new ZapfprofilAuslegungEingabeDaten { PunktVolumenL = 300, PunktLeistungKw = 25 };
        var cut = Aufbauen(Daten(e));
        Stufe(cut, "Erweitert");

        Feld(cut, "Quelle des Messwerts").Input("Zähler");
        Assert.False(cut.Instance.Eingabe.PunktUeberholt);
        Feld(cut, "Ladezeitfenster").Input("6");
        Assert.True(cut.Instance.Eingabe.PunktUeberholt);
        Assert.Null(cut.Instance.Eingabe.Auslegung!.PunktVolumenL);
    }

    // =================================================================================
    // Rechte Seite
    // =================================================================================

    [Fact]
    public void Die_Warnliste_zeigt_Stufe_Titel_und_Satz()
    {
        var cut = Aufbauen();

        IElement liste = cut.Find(".epos-zapfprofil-warnliste");
        Assert.Contains("Hinweise der Bilanz", liste.TextContent);
        IElement[] eintraege = liste.QuerySelectorAll("li").ToArray();
        Assert.Equal(2, eintraege.Length);
        Assert.Equal("hinweis", eintraege[0].GetAttribute("data-stufe"));
        Assert.Contains("Hinweis", eintraege[0].QuerySelector(".epos-zapfausl-stufe")!.TextContent);
        Assert.Equal("ZPG_WARN_NETZVERLUST_UND_ZIRKULATION", eintraege[1].GetAttribute("data-kennung"));
        Assert.Contains("epos-zapfausl-stufe--warnung", eintraege[1].QuerySelector(".epos-zapfausl-stufe")!.ClassName);
        Assert.Contains("Netzverluste und Zirkulation:", eintraege[1].TextContent);
        Assert.Contains("Doppelt gezählt?", eintraege[1].TextContent);
    }

    [Fact]
    public void Der_Reiter_Dauerlinie_zeigt_ab_Erweitert_Marken_und_Schwelle()
    {
        var cut = Aufbauen();
        Stufe(cut, "Erweitert");

        IElement dauer = cut.FindAll("[role=tab]").First(b => b.TextContent.Trim() == "Dauerlinie");
        Assert.Null(dauer.GetAttribute("aria-disabled"));
        dauer.Click();
        Assert.Equal("dauerlinie", cut.Instance.AktiverReiter);
        Assert.Contains("P99 · 13,5 kW (Rang 88) · P95 · 10,6 kW (Rang 438)", cut.Find(".epos-zapfprofil-dauermarken").TextContent);
        Assert.Contains("600 Stunden über 1,0 kW", cut.Find(".epos-zapfprofil-dauerschwelle").TextContent);

        // Zurück in Einfach: der Reiter ist gesperrt, die Vorschau steht wieder auf dem Tagesgang.
        Stufe(cut, "Einfach");
        Assert.Equal("tagesgang", cut.Instance.AktiverReiter);
    }

    [Fact]
    public void Die_Auslegung_bekommt_die_Stufe_des_Dialogs()
    {
        var stufen = new List<ZapfprofilStufe>();
        var cut = Aufbauen(auslegung: (e, _) => { stufen.Add(e.Stufe); return new Dictionary<string, object>(); });

        Knopf(cut, "Auslegung…").Click();
        Stufe(cut, "Experte");
        Knopf(cut, "Auslegung…").Click();
        Assert.Equal(new[] { ZapfprofilStufe.Einfach, ZapfprofilStufe.Experte }, stufen);
    }

    // =================================================================================
    // Der Hilfe-Assistent
    // =================================================================================

    private static WindowsFormsApplication1.KiFeldzugang Zugang(string feld)
        => WindowsFormsApplication1.KiMaskenbruecke.Feldzugang(WindowsFormsApplication1.KiMaskennamen.ZAPFPROFIL, feld);

    private static void Setze(string feld, string text)
    {
        WindowsFormsApplication1.KiFeldzugang z = Zugang(feld);
        Assert.NotNull(z);
        WindowsFormsApplication1.KiFeldumsetzung u = WindowsFormsApplication1.KiFeldwandler.Wandle(z, text);
        Assert.True(u.Ok, u.Grund);
        z.Setzen(u.Wert);
    }

    [Fact]
    public void Der_Assistent_setzt_die_Angaben_der_hoeheren_Stufen_nur_wo_die_Maske_sie_zeigt()
    {
        WindowsFormsApplication1.KiMaskenbruecke.Leeren();
        var cut = Aufbauen();
        var texte = new ZapfprofilTexte();

        var einfach = Assert.Throws<InvalidOperationException>(() => Zugang("topologie").Setzen(3));
        Assert.Contains(texte.StufeErweitert, einfach.Message, StringComparison.Ordinal);

        Setze("stufe", "Erweitert");
        cut.Render();
        Setze("topologie", "Durchfluss");
        Setze("ladefenster", "7");
        Assert.Equal(ZapfprofilTopologie.Durchfluss, cut.Instance.Eingabe.Zonen[0].Angaben!.Topologie);
        Assert.Equal(7.0, cut.Instance.Eingabe.Gebaeude!.LadefensterH);
        Assert.Throws<InvalidOperationException>(() => Zugang("ladefenster").Setzen(30.0));

        // Die Methode bestimmt, welche Zirkulationsfelder stehen.
        var laenge = Assert.Throws<InvalidOperationException>(() => Zugang("zirk_laenge").Setzen(50.0));
        Assert.Contains(texte.ZirkMethodeLaenge, laenge.Message, StringComparison.Ordinal);
        Setze("zirk_methode", texte.ZirkMethodeLaenge);
        Setze("zirk_laenge", "50");
        Assert.Equal(50.0, cut.Instance.Eingabe.Gebaeude!.ZirkLaengeM);

        // Fachwerte erst in Experte.
        var experte = Assert.Throws<InvalidOperationException>(() => Zugang("zapftemperatur").Setzen(55.0));
        Assert.Contains(texte.StufeExperte, experte.Message, StringComparison.Ordinal);
        Setze("stufe", "Experte");
        cut.Render();
        Setze("zapftemperatur", "55");
        Assert.Equal(55.0, cut.Instance.Eingabe.Zonen[0].Angaben!.ZapftemperaturC);
        EPOS.UI.Tests.Dialoge.Hilfe.KiReihenhilfe.Setze(WindowsFormsApplication1.KiMaskennamen.ZAPFPROFIL, "auslastungsgang", new[] { 1.1 }, ab: 1);
        EPOS.UI.Tests.Dialoge.Hilfe.KiReihenhilfe.Setze(WindowsFormsApplication1.KiMaskennamen.ZAPFPROFIL, "auslastungsgang", new[] { 0.5 }, ab: 8);   // Stellen zählen ab 1: August
        Assert.Equal(1.1, cut.Instance.Eingabe.Zonen[0].Angaben!.Auslastung[0]);
        Assert.Equal(0.5, cut.Instance.Eingabe.Zonen[0].Angaben!.Auslastung[7]);

        // Ferien als Tabelle: Die Zeile hinter dem letzten Zeitraum legt einen neuen an; von und bis als „TT.MM.".
        int vorher = cut.Instance.Eingabe.Zonen[0].Angaben!.Ferien.Count;
        string neu = (vorher + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Setze("ferien_von_" + neu, "01.07.");
        Setze("ferien_bis_" + neu, "14.08.");
        Setze("ferien_name_" + neu, "Sommer");
        ZapfprofilFerienDaten sommer = cut.Instance.Eingabe.Zonen[0].Angaben!.Ferien[vorher];
        Assert.Equal(vorher + 1, cut.Instance.Eingabe.Zonen[0].Angaben!.Ferien.Count);
        Assert.Equal(("Sommer", 182, 226), (sommer.Name, sommer.Von, sommer.Bis));
        Assert.Throws<InvalidOperationException>(() => Zugang("ferien_bis_" + neu).Setzen("30.02."));
        Assert.Equal(226, sommer.Bis);

        // Die Wohnungstabelle der gewählten Zone.
        // Der Assistent setzt auch in einer zugeklappten Karte (die Stufe zählt, nicht die Klappe); den Knopf
        // „Wohnungstyp hinzufügen" drückt hier der Anwender - dafür klappt er die Karte „Belegung" auf.
        cut.Find("table.epos-zapfprofil-zonen tbody tr button").Click();
        ZapfprofilKarten.Auf(cut, "belegung");
        cut.InvokeAsync(() => Knopf(cut, "Wohnungstyp hinzufügen").Click());
        Setze("wohnung_anzahl_1", "5");
        Assert.Equal(5, cut.Instance.Eingabe.Zonen[0].Angaben!.Wohnungen[0].Anzahl);
        Setze("wohnung_ausstattung_1", "Testklasse A");
        Assert.Equal(3, cut.Instance.Eingabe.Zonen[0].Angaben!.Wohnungen[0].IdAusstattung);
    }
}
