using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Der Reiter „Konditionierung" und seine Vorgabe-Matrix</b> (Stufe KP2, Welle U1; Teilkonzept
/// Konditionierungsprofile 7.1, 7.2; Entwurf KP2 Festlegungen 1, 3, 6, 7; E56 F2 (a), F3 (a), F5 (a)).
///
/// <para><b>Was geprüft wird:</b> der Feldbestand der Matrix (fünf Spalten, sechs Zeilen, die Zellen,
/// die es gibt, und „—" für die übrigen); ohne Weg (ohne Gaben, ohne Konditionierungstabellen) sind
/// die Bestandszellen bedienbar und schreiben ihr Feld, der Grund steht als leise Zeile, Karten und
/// Handlungen fehlen; die schmale Anordnung (fünf Reiter, Klassen der gewählten Spalte — das
/// Umbrechen selbst prüft <c>StilblattTests</c>, gemessen wird es in der Konditionierungsprobe); der
/// Lesemodus eines ausgelieferten Satzes; die weich gesperrte Kühlspalte; der Tagesbilanz-Weg; mit
/// Weg: eine neue Zelle über „Zelle setzen", Eingaben in dieselbe Zelle als EIN Schritt und
/// „Zurücknehmen" (eine Stufe), „Kalender anlegen", „Verwerfen" und „Matrix erneut anwenden…" mit
/// Rückfrage VOR der Handlung (Vorgabe Nein, Posten und Zonen mit Namen), die Rückfrage „aufteilen"
/// (F5 (a)) als ein Schritt, „Aus dem Katalog erneut übernehmen…" nur im Projekt — und kein Knopf ohne
/// Delegat.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt (deutsche Rückfalltexte und Zahlformat).</para>
/// </summary>
public class KonditionierungMatrixTests : EposBunitContext
{
    public KonditionierungMatrixTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private KonditionierungBearbeitung _bearbeitung = default!;

    private readonly List<string> _meldungen = new();

    /// <summary>Ein Gebäudesatz mit Heizwerten und Nachtfenster, gekühlt.</summary>
    private static GebaeudeKatalogDaten Satz(bool mitKonditionierung = false)
    {
        var d = new GebaeudeKatalogDaten
        {
            Name = "Büro Nord",
            SollTag = 20,
            NachtAbsenkung = 17,
            NachtBeginn = 22,
            NachtEnde = 6,
            KuehlungAktiv = true,
            KuehlSollwert = 26,
            Waermegewinne = 4
        };
        if (mitKonditionierung) d.Konditionierung = new KonditionierungDaten();
        return d;
    }

    private IRenderedComponent<KonditionierungReiter> Aufbauen(
        GebaeudeKatalogDaten satz, KonditionierungWeg? weg = null, bool lesemodus = false, string? kuehlsperre = null,
        bool altweg = false, bool projekt = false)
    {
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(satz, neu: false);
        _bearbeitung = new KonditionierungBearbeitung(arbeit, () => weg) { Melden = (m, _) => _meldungen.Add(m) };
        return Render<KonditionierungReiter>(p => p
            .Add(x => x.Bearbeitung, _bearbeitung)
            .Add(x => x.Lesemodus, lesemodus)
            .Add(x => x.Kuehlsperre, kuehlsperre)
            .Add(x => x.Altweg, altweg)
            .Add(x => x.Projekt, projekt));
    }

    /// <summary>Das Eingabefeld der Zelle mit diesem Namen („Heizen · Tag").</summary>
    private static IElement Eingabe(IRenderedComponent<KonditionierungReiter> cut, string feld)
        => cut.FindAll("label.epos-feld")
              .First(l => l.QuerySelector(".epos-feld-text")?.TextContent.Trim() == feld)
              .QuerySelector("input")!;

    private static bool HatFeld(IRenderedComponent<KonditionierungReiter> cut, string feld)
        => cut.FindAll("label.epos-feld .epos-feld-text").Any(t => t.TextContent.Trim() == feld);

    private static IElement Zelle(IRenderedComponent<KonditionierungReiter> cut, KonditionierungGroesse g, KonditionierungZeile z)
        => cut.Find($"td.epos-kond-zelle[data-groesse='{(int)g}'][data-zeile='{(int)z}']");

    private static IElement Knopf(IRenderedComponent<KonditionierungReiter> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Ein Weg wie der Kern, im Kleinen
    // =================================================================================

    /// <summary>Was der Weg gesehen hat.</summary>
    private sealed class Protokoll
    {
        public List<string> Aufrufe { get; } = new();
    }

    /// <summary>
    /// Ein Weg, der Zellen einträgt (Bestandszellen auch in ihr Feld), Kalender anlegt, verwirft und neu
    /// aus der Matrix anwendet; <paramref name="aufteilen"/>: die Lüftung einer Gesamtangabe fragt F5.
    /// </summary>
    private static KonditionierungWeg Weg(Protokoll p, bool aufteilen = false, bool nurZellen = false,
                                          KonditionierungRueckfrage? befund = null)
    {
        KonditionierungErgebnis Zelle(KonditionierungStand s, KonditionierungOrt o, KonditionierungZeile z, KonditionierungZelle c)
        {
            p.Aufrufe.Add("Zelle " + o.Groesse + " " + z);
            GebaeudeKatalogDaten g = s.Gebaeude.Kopie();
            if (aufteilen && o.Groesse == KonditionierungGroesse.Lueftung && g.Luftwechselrate is not null
                && g.LuftwechselInfiltration is null)
                return KonditionierungErgebnis.Frage(new KonditionierungRueckfrage(
                    new[] { new KonditionierungPosten(KonditionierungPostenart.Luftwechsel, 1) },
                    Array.Empty<KonditionierungPosten>(), Array.Empty<string>()));
            g.Konditionierung ??= new KonditionierungDaten();
            KonditionierungZelle ziel = g.Konditionierung.Spalte(o.Groesse).Zelle(z);
            ziel.Wert = c.Wert;
            ziel.Aus = c.Aus;
            ziel.Von = c.Von;
            ziel.Bis = c.Bis;
            ziel.DeltaT = c.DeltaT;
            if (o.Groesse == KonditionierungGroesse.Heizen && z == KonditionierungZeile.Tag && c.Wert is double w) g.SollTag = w;
            g.Konditionierung.Weiterzaehlen();
            return KonditionierungErgebnis.Gut(new KonditionierungStand(g, s.Zonen));
        }

        KonditionierungErgebnis Kalender(KonditionierungStand s, KonditionierungOrt o, string was, bool angelegt)
        {
            p.Aufrufe.Add(was + " " + o.Groesse);
            GebaeudeKatalogDaten g = s.Gebaeude.Kopie();
            g.Konditionierung ??= new KonditionierungDaten();
            var k = new KonditionierungKalender();
            if (angelegt)
            {
                k.Zustand = KonditionierungZustand.Angelegt;
                k.Perioden.Add(new KonditionierungPeriode { Rang = 100, Matrixbereich = true });
                for (int i = 0; i < 3; i++) k.Perioden.Add(new KonditionierungPeriode { Rang = 300 + i, Name = "P" + i });
            }
            g.Konditionierung.Spalte(o.Groesse).Kalender = k;
            g.Konditionierung.Weiterzaehlen();
            return KonditionierungErgebnis.Gut(new KonditionierungStand(g, s.Zonen));
        }

        if (nurZellen) return new KonditionierungWeg { ZelleSetzen = Zelle };

        return new KonditionierungWeg
        {
            ZelleSetzen = Zelle,
            Anlegen = (s, o) => Kalender(s, o, "Anlegen", true),
            Verwerfen = (s, o) => Kalender(s, o, "Verwerfen", false),
            MatrixErneut = (s, o) => Kalender(s, o, "MatrixErneut", true),
            KatalogErneut = s =>
            {
                p.Aufrufe.Add("KatalogErneut");
                GebaeudeKatalogDaten g = s.Gebaeude.Kopie();
                g.SollTag = 21;
                return KonditionierungErgebnis.Gut(new KonditionierungStand(g, s.Zonen));
            },
            LuftwechselAufteilen = s =>
            {
                p.Aufrufe.Add("Aufteilen");
                GebaeudeKatalogDaten g = s.Gebaeude.Kopie();
                g.LuftwechselInfiltration = 0.2;
                g.LuftwechselNutzer = 0.4;
                g.Luftwechselrate = null;
                return KonditionierungErgebnis.Gut(new KonditionierungStand(g, s.Zonen));
            },
            Rueckfrage = (_, _, h) =>
            {
                p.Aufrufe.Add("Rückfrage " + h);
                return befund;
            }
        };
    }

    // =================================================================================
    // Feldbestand und der Fall ohne Weg
    // =================================================================================

    [Fact]
    public void Die_Matrix_hat_fuenf_Spalten_sechs_Zeilen_und_ohne_Weg_die_Bestandszellen()
    {
        var cut = Aufbauen(Satz());

        Assert.Equal(new[] { "Heizen °C", "Kühlen °C", "Lüftung 1/h", "Geräte W bzw. %", "Personen W bzw. %" },
                     cut.FindAll("table.epos-kond-matrix th[scope=col]").Select(t => t.TextContent.Trim()));
        Assert.Equal(new[] { "Nennwert", "Tag", "Nacht", "Wochenende", "Ferien", "Saison" },
                     cut.FindAll("table.epos-kond-matrix th[scope=row]").Select(t => t.TextContent.Trim()));
        Assert.Equal(30, cut.FindAll("td.epos-kond-zelle").Count);

        // Ohne Weg: die neun Bestandszellen und das Nachtfenster der Heizspalte - sonst „—".
        string[] erwartet =
        {
            "Heizen · Tag", "Heizen · Nacht", "Heizen · Nachtfenster", "Soll am Wochenende (ganztägig)",
            "Soll in Ferien (ganztägig)", "Kühlen · Tag", "Kühlen · Nacht", "Lüftung · Infiltration",
            "Lüftung · Nutzerlüftung", "Geräte · Nennwert"
        };
        List<string> felder = cut.FindAll("table.epos-kond-matrix label.epos-feld .epos-feld-text")
                                 .Select(t => t.TextContent.Trim()).ToList();
        Assert.Equal(erwartet.OrderBy(x => x), felder.OrderBy(x => x));
        Assert.Equal("—", Zelle(cut, KonditionierungGroesse.Personen, KonditionierungZeile.Tag).TextContent.Trim());
        Assert.Equal("—", Zelle(cut, KonditionierungGroesse.Heizen, KonditionierungZeile.Nennwert).TextContent.Trim());
        Assert.Equal("—", Zelle(cut, KonditionierungGroesse.Geraete, KonditionierungZeile.Saison).TextContent.Trim());

        Assert.Equal("20", Eingabe(cut, "Heizen · Tag").GetAttribute("value"));
        Assert.Equal("22–6", Eingabe(cut, "Heizen · Nachtfenster").GetAttribute("value"));
        Assert.Equal("26", Eingabe(cut, "Kühlen · Tag").GetAttribute("value"));

        // Benannt gesperrt: der Grund als leise Zeile, keine Karten, keine Handlungen.
        Assert.Contains("Die Konditionierung steht nicht zur Verfügung", cut.Find("p.epos-kond-sperrzeile").TextContent);
        Assert.Empty(cut.FindAll("section.epos-kond-karte"));
        Assert.Empty(cut.FindAll("button.epos-kond-zuruecknehmen"));
        Assert.Empty(cut.FindAll("button.epos-kond-katalog-erneut"));
    }

    [Fact]
    public void Ohne_Weg_schreibt_eine_Bestandszelle_ihr_Feld()
    {
        var cut = Aufbauen(Satz());

        Eingabe(cut, "Heizen · Tag").Input("21,5");
        Eingabe(cut, "Lüftung · Infiltration").Input("0.3");
        Eingabe(cut, "Heizen · Nachtfenster").Input("23-5");
        Eingabe(cut, "Kühlen · Tag").Input("");

        Assert.Equal(21.5, _bearbeitung.Stand.SollTag);
        Assert.Equal(0.3, _bearbeitung.Stand.LuftwechselInfiltration);
        Assert.Equal(23, _bearbeitung.Stand.NachtBeginn);
        Assert.Equal(5, _bearbeitung.Stand.NachtEnde);
        Assert.Null(_bearbeitung.Stand.KuehlSollwert);
        Assert.Null(_bearbeitung.Stand.Konditionierung);
    }

    [Fact]
    public void Ohne_Gaben_zeichnet_der_Reiter_mit_den_Rueckfaellen()
    {
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(null, neu: true);
        var cut = Render<KonditionierungReiter>(p => p.Add(x => x.Bearbeitung, new KonditionierungBearbeitung(arbeit, () => null)));

        Assert.Single(cut.FindAll("table.epos-kond-matrix"));
        Assert.Equal("22–6", Eingabe(cut, "Heizen · Nachtfenster").GetAttribute("placeholder"));
        Assert.Equal("", Eingabe(cut, "Heizen · Tag").GetAttribute("value") ?? "");
        Assert.NotNull(cut.Find("p.epos-kond-sperrzeile"));
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    // =================================================================================
    // Anordnung: schmal fünf Reiter, breit die ganze Tabelle (dasselbe Markup)
    // =================================================================================

    [Fact]
    public void Die_Reiter_der_schmalen_Anordnung_waehlen_Spalte_und_Karte()
    {
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(new Protokoll()));

        List<IElement> reiter = cut.FindAll(".epos-kond-groessen button[role=tab]").ToList();
        Assert.Equal(new[] { "Heizen", "Kühlen", "Lüftung", "Geräte", "Personen" }, reiter.Select(r => r.TextContent.Trim()));
        Assert.Equal("true", reiter[0].GetAttribute("aria-selected"));
        Assert.Equal("0", cut.Find("table.epos-kond-matrix").GetAttribute("data-aktiv"));

        reiter[2].Click();

        Assert.Equal(KonditionierungGroesse.Lueftung, cut.Instance.AktiveGroesse);
        Assert.Equal("2", cut.Find("table.epos-kond-matrix").GetAttribute("data-aktiv"));
        Assert.Equal("true", cut.FindAll(".epos-kond-groessen button[role=tab]")[2].GetAttribute("aria-selected"));
        Assert.Equal("-1", cut.FindAll(".epos-kond-groessen button[role=tab]")[0].GetAttribute("tabindex"));
        Assert.All(cut.FindAll("td.epos-kond-zelle.epos-kond--aktiv"), z => Assert.Equal("2", z.GetAttribute("data-groesse")));
        Assert.Equal(6, cut.FindAll("td.epos-kond-zelle.epos-kond--aktiv").Count);
        Assert.Equal("2", cut.Find("th[scope=col].epos-kond--aktiv").GetAttribute("data-groesse"));
        Assert.Equal("2", cut.Find("section.epos-kond-karte.epos-kond--aktiv").GetAttribute("data-groesse"));

        // Die Zellen aller Spalten stehen trotzdem im Markup - breit zeigt das Stilblatt sie.
        Assert.Equal(30, cut.FindAll("td.epos-kond-zelle").Count);
        Assert.Equal(5, cut.FindAll("section.epos-kond-karte").Count);
    }

    // =================================================================================
    // Lesemodus, weiche Kühlsperre, Tagesbilanz-Weg
    // =================================================================================

    [Fact]
    public void Im_Lesemodus_stehen_die_Werte_als_Text_ohne_Handlung()
    {
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(new Protokoll()), lesemodus: true, projekt: true);

        Assert.Empty(cut.FindAll("table.epos-kond-matrix input"));
        Assert.Equal("20", cut.Find("span.epos-kond-text[data-feld='Heizen · Tag']").TextContent.Trim());
        Assert.Equal("17 (22–6)", cut.Find("span.epos-kond-text[data-feld='Heizen · Nacht']").TextContent.Trim());
        Assert.Equal("keine", cut.Find("span.epos-kond-text[data-feld='Soll am Wochenende (ganztägig)']").TextContent.Trim());
        Assert.Equal("ganzjährig", cut.Find("span.epos-kond-text[data-feld='Heizen · Saison']").TextContent.Trim());

        Assert.Empty(cut.FindAll(".epos-kond-kopf button"));
        Assert.Empty(cut.FindAll("section.epos-kond-karte button"));
        Assert.Equal(5, cut.FindAll("section.epos-kond-karte").Count);
        Assert.All(cut.FindAll(".epos-kond-zusatz input, .epos-kond input"),
                   e => Assert.True(e.HasAttribute("readonly") || e.HasAttribute("disabled")));
    }

    [Fact]
    public void Die_Kuehlspalte_ist_ohne_Kuehlung_weich_gesperrt_und_nennt_den_Grund()
    {
        const string grund = "Die Kühlspalte gilt erst mit „Gebäude wird gekühlt“.";
        var cut = Aufbauen(Satz(), kuehlsperre: grund);

        Assert.False(HatFeld(cut, "Kühlen · Tag"));
        IElement knopf = Zelle(cut, KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag).QuerySelector("button")!;
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.False(knopf.HasAttribute("disabled"));
        Assert.Equal(grund, knopf.GetAttribute("title"));
        Assert.Equal("Kühlen · Tag: 26", knopf.GetAttribute("aria-label"));

        knopf.Click();

        Assert.Equal(new[] { grund }, _meldungen);
        Assert.Equal(26.0, _bearbeitung.Stand.KuehlSollwert);
        Assert.Contains(grund, cut.Find("p.epos-kond-kuehlgrund").TextContent);
    }

    [Fact]
    public void Auf_dem_Tagesbilanz_Weg_stehen_nur_die_Felder_des_Altwegs()
    {
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(new Protokoll()), altweg: true);

        List<string> felder = cut.FindAll("table.epos-kond-matrix label.epos-feld .epos-feld-text")
                                 .Select(t => t.TextContent.Trim()).OrderBy(x => x).ToList();
        Assert.Equal(new[] { "Geräte · Nennwert", "Heizen · Nacht", "Heizen · Tag", "Soll am Wochenende (ganztägig)",
                             "Soll in Ferien (ganztägig)" }, felder);
        Assert.Equal("—", Zelle(cut, KonditionierungGroesse.Kuehlen, KonditionierungZeile.Tag).TextContent.Trim());
        Assert.NotNull(cut.Find("p.epos-kond-altweg"));
        Assert.Empty(cut.FindAll("section.epos-kond-karte"));
    }

    // =================================================================================
    // Mit Weg: Zellen, „Zurücknehmen", Karten, Rückfragen
    // =================================================================================

    [Fact]
    public void Mit_Weg_geht_eine_neue_Zelle_ueber_Zelle_setzen_und_Zuruecknehmen_nimmt_einen_Schritt()
    {
        var p = new Protokoll();
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(p));

        Assert.True(HatFeld(cut, "Geräte · Tag"));
        Assert.True(HatFeld(cut, "Lüftung · Nachtfenster"));
        Assert.Equal("true", cut.Find("button.epos-kond-zuruecknehmen").GetAttribute("aria-disabled"));

        // Zwei Eingaben in dieselbe Zelle sind EIN Schritt.
        Eingabe(cut, "Geräte · Tag").Input("80");
        Eingabe(cut, "Geräte · Tag").Input("70");
        Assert.Equal(new[] { "Zelle Geraete Tag", "Zelle Geraete Tag" }, p.Aufrufe);
        Assert.Equal(70.0, _bearbeitung.Wert(KonditionierungGroesse.Geraete, KonditionierungZeile.Tag));

        cut.Render();
        IElement zurueck = cut.Find("button.epos-kond-zuruecknehmen");
        Assert.Null(zurueck.GetAttribute("aria-disabled"));
        zurueck.Click();

        Assert.Null(_bearbeitung.Wert(KonditionierungGroesse.Geraete, KonditionierungZeile.Tag));
        Assert.False(_bearbeitung.KannZuruecknehmen);

        // Eine Stufe: ein zweites „Zurücknehmen" meldet, dass es nichts gibt.
        cut.Find("button.epos-kond-zuruecknehmen").Click();
        Assert.Contains("keinen Schritt", _meldungen.Single());
    }

    [Fact]
    public void Eine_Bestandszelle_geht_mit_Weg_ueber_den_Weg_und_leer_leert_ihr_Feld()
    {
        var p = new Protokoll();
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(p));

        Eingabe(cut, "Heizen · Tag").Input("22");
        Assert.Equal(new[] { "Zelle Heizen Tag" }, p.Aufrufe);
        Assert.Equal(22.0, _bearbeitung.Stand.SollTag);

        Eingabe(cut, "Heizen · Tag").Input("");
        Assert.Null(_bearbeitung.Stand.SollTag);
    }

    [Fact]
    public void Kalender_anlegen_verwerfen_und_Matrix_erneut_mit_Rueckfrage_vor_der_Handlung()
    {
        var p = new Protokoll();
        var befund = new KonditionierungRueckfrage(
            new[] { new KonditionierungPosten(KonditionierungPostenart.Kalender, 1),
                    new KonditionierungPosten(KonditionierungPostenart.EigenePerioden, 3) },
            new[] { new KonditionierungPosten(KonditionierungPostenart.Matrixzellen, 9) },
            new[] { "Büro", "Lager" });
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(p, befund: befund));

        IElement karte = cut.Find("section.epos-kond-karte[data-groesse='0']");
        Assert.Equal("aus der Matrix", karte.QuerySelector(".epos-kond-karte-zustand")!.TextContent.Trim());
        Assert.Empty(karte.QuerySelectorAll("button.epos-kond-verwerfen"));

        cut.Find("section.epos-kond-karte[data-groesse='0'] button.epos-kond-anlegen").Click();
        Assert.Equal(new[] { "Anlegen Heizen" }, p.Aufrufe);
        Assert.Equal("angelegt, 3 eigene Perioden",
                     cut.Find("section.epos-kond-karte[data-groesse='0'] .epos-kond-karte-zustand").TextContent.Trim());
        Assert.Empty(cut.FindAll("section.epos-kond-karte[data-groesse='0'] button.epos-kond-anlegen"));

        // Verwerfen: erst die Frage - mit Posten und Zonen -, Vorgabe „Nein"; „Nein" lässt alles.
        cut.Find("section.epos-kond-karte[data-groesse='0'] button.epos-kond-verwerfen").Click();
        Assert.Equal(new[] { "Anlegen Heizen", "Rückfrage Verwerfen" }, p.Aufrufe);
        string frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Equal("Den angelegten Kalender „Heizen“ verwerfen? Es fällt: 1 Kalender, 3 eigene Perioden. " +
                     "Es bleibt: 9 Zellen der Matrix. Danach gilt wieder die Matrix. Betroffene Zonen: Büro, Lager.",
                     frage);
        IElement nein = Knopf(cut, "Nein");
        Assert.Contains("epos-knopf--primaer", nein.ClassName);
        nein.Click();
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.True(_bearbeitung.Angelegt(KonditionierungGroesse.Heizen));

        // „Ja" verwirft.
        cut.Find("section.epos-kond-karte[data-groesse='0'] button.epos-kond-verwerfen").Click();
        Knopf(cut, "Ja").Click();
        Assert.Equal("Verwerfen Heizen", p.Aufrufe[^1]);
        Assert.False(_bearbeitung.Angelegt(KonditionierungGroesse.Heizen));

        // „Matrix erneut anwenden…" an einem angelegten Kalender - ebenfalls mit Frage.
        cut.Find("section.epos-kond-karte[data-groesse='3'] button.epos-kond-anlegen").Click();
        cut.Find("section.epos-kond-karte[data-groesse='3'] button.epos-kond-matrix-erneut").Click();
        Assert.StartsWith("Die Matrix erneut auf den Kalender „Geräte“ anwenden? Ersetzt wird: 1 Kalender",
                          cut.Find(".epos-rueckfrage-text").TextContent);
        Knopf(cut, "Ja").Click();
        Assert.Equal("MatrixErneut Geraete", p.Aufrufe[^1]);
    }

    [Fact]
    public void Ohne_Befund_geht_die_Handlung_ohne_Frage_weiter()
    {
        var p = new Protokoll();
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(p, befund: null));

        cut.Find("section.epos-kond-karte[data-groesse='1'] button.epos-kond-anlegen").Click();
        cut.Find("section.epos-kond-karte[data-groesse='1'] button.epos-kond-verwerfen").Click();

        Assert.Equal(new[] { "Anlegen Kuehlen", "Rückfrage Verwerfen", "Verwerfen Kuehlen" }, p.Aufrufe);
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    [Fact]
    public void Die_Gesamtangabe_der_Lueftung_fragt_aufteilen_und_teilt_in_einem_Schritt()
    {
        var p = new Protokoll();
        GebaeudeKatalogDaten satz = Satz(mitKonditionierung: true);
        satz.Luftwechselrate = 0.6;
        var cut = Aufbauen(satz, Weg(p, aufteilen: true));

        Eingabe(cut, "Lüftung · Nachtauskühlung").Input("2");
        cut.Render();

        Assert.Equal(new[] { "Zelle Lueftung Nacht", "Aufteilen" }, p.Aufrufe);
        Assert.Null(_bearbeitung.Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht));
        string frage = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Contains("Luftwechselrate 0,6 1/h", frage);
        Assert.Contains("Infiltration 0,2 1/h und Nutzerlüftung 0,4 1/h", frage);
        // Aufteilen ersetzt nichts, was verloren ginge - „Ja" ist die Vorgabe.
        Assert.Contains("epos-knopf--primaer", Knopf(cut, "Ja").ClassName);

        Knopf(cut, "Ja").Click();

        Assert.Equal(0.2, _bearbeitung.Stand.LuftwechselInfiltration);
        Assert.Equal(0.4, _bearbeitung.Stand.LuftwechselNutzer);
        Assert.Null(_bearbeitung.Stand.Luftwechselrate);
        Assert.Equal(2.0, _bearbeitung.Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht));

        // EIN Schritt: „Zurücknehmen" stellt Gesamtangabe und leere Zelle wieder her.
        cut.Find("button.epos-kond-zuruecknehmen").Click();
        Assert.Equal(0.6, _bearbeitung.Stand.Luftwechselrate);
        Assert.Null(_bearbeitung.Stand.LuftwechselInfiltration);
        Assert.Null(_bearbeitung.Wert(KonditionierungGroesse.Lueftung, KonditionierungZeile.Nacht));
    }

    [Fact]
    public void Aus_dem_Katalog_erneut_uebernehmen_steht_nur_im_Projekt_und_fragt_immer()
    {
        var p = new Protokoll();
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(p));
        Assert.Empty(cut.FindAll("button.epos-kond-katalog-erneut"));

        cut = Aufbauen(Satz(mitKonditionierung: true), Weg(p), projekt: true);
        cut.Find("button.epos-kond-katalog-erneut").Click();

        // Ohne Befund fragt es trotzdem - die ganze Gebäudeebene wird ersetzt.
        Assert.StartsWith("Die Konditionierung des Gebäudes erneut aus dem Katalogsatz übernehmen?",
                          cut.Find(".epos-rueckfrage-text").TextContent);
        Assert.Contains("epos-knopf--primaer", Knopf(cut, "Nein").ClassName);
        Knopf(cut, "Ja").Click();
        Assert.Equal("KatalogErneut", p.Aufrufe[^1]);
        Assert.Equal(21.0, _bearbeitung.Stand.SollTag);
    }

    [Fact]
    public void Kein_Delegat_kein_Knopf()
    {
        var cut = Aufbauen(Satz(mitKonditionierung: true), Weg(new Protokoll(), nurZellen: true), projekt: true);

        Assert.Equal(5, cut.FindAll("section.epos-kond-karte").Count);
        Assert.Empty(cut.FindAll("section.epos-kond-karte button"));
        Assert.Empty(cut.FindAll("button.epos-kond-katalog-erneut"));
        Assert.Single(cut.FindAll("button.epos-kond-zuruecknehmen"));
        Assert.Empty(cut.FindAll("p.epos-kond-sperrzeile"));

        // Eine Sperre des Wegs nimmt alles und nennt ihren Grund.
        var gesperrt = new KonditionierungWeg { ZelleSetzen = (s, _, _, _) => KonditionierungErgebnis.Gut(s), Sperre = "Gesperrt, weil." };
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(Satz(mitKonditionierung: true), neu: false);
        var zweiter = Render<KonditionierungReiter>(q => q.Add(x => x.Bearbeitung, new KonditionierungBearbeitung(arbeit, () => gesperrt)));
        Assert.Equal("Gesperrt, weil.", zweiter.Find("p.epos-kond-sperrzeile").TextContent.Trim());
        Assert.Empty(zweiter.FindAll("section.epos-kond-karte"));
        Assert.False(zweiter.FindAll("label.epos-feld .epos-feld-text").Any(t => t.TextContent.Trim() == "Geräte · Tag"));
    }
}
