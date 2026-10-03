using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Standards;
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
/// Delegat. Dazu die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…" (E57, Welle U5): die Liste
/// „alle Größen" in der Kopfzelle der Zeile „Vorlage" (Einträge, Reihenfolge, wo sie fehlt), die EINE Rückfrage
/// mit je Größe einer Zeile samt Sperre und Zonen, und der Fehler eines Schritts, der nichts schreibt.</para>
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

    /// <summary>
    /// Die Felder der Kühlspalte nehmen die Zellgrenze des Kerns (<c>Konditionierungsgroessen.Min/Max</c>) — dieselbe
    /// Zahl, mit der die Zellprüfung ablehnt und die die Meldung nennt.
    /// </summary>
    [Fact]
    public void Die_Felder_der_Kuehlspalte_nehmen_die_Zellgrenze_des_Kerns()
    {
        var cut = Aufbauen(Satz());
        double min = Konditionierungsgroessen.Min(Konditionierungsgroesse.Kuehlsoll);
        double max = Konditionierungsgroessen.Max(Konditionierungsgroesse.Kuehlsoll);
        var kuehlfelder = cut.FindComponents<Zahlenfeld>().Where(f => f.Instance.Bezeichnung.StartsWith("Kühlen ·")).ToList();
        Assert.NotEmpty(kuehlfelder);
        Assert.All(kuehlfelder, f => Assert.Equal(((double?)min, (double?)max), (f.Instance.Min, f.Instance.Max)));

        // Genau an der Grenze nimmt das Feld den Wert, knapp darüber färbt es.
        Eingabe(cut, "Kühlen · Tag").Input(max.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(max, _bearbeitung.Stand.KuehlSollwert);
        Eingabe(cut, "Kühlen · Tag").Input((max + 0.5).ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("epos-fehleingabe", Eingabe(cut, "Kühlen · Tag").ClassName);
        Assert.Equal(max, _bearbeitung.Stand.KuehlSollwert);
        Assert.NotNull(Konditionierungsarbeit.Zellenpruefung(Konditionierungsgroesse.Kuehlsoll, DbWerte.KOND_ZEILE_TAG,
                                                              Matrixzelle.AusWert(max + 0.5)));
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

    // =================================================================================
    // Die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…" (E57; Stufe KP2, Welle U5)
    // =================================================================================

    /// <summary>Die Liste „alle Größen" in der Kopfzelle der Zeile „Vorlage"; <c>null</c> = keine.</summary>
    private static IElement? ListeAlle(IRenderedComponent<KonditionierungReiter> cut)
        => cut.FindAll("table.epos-kond-matrix tr[data-zeile='vorlage'] > th[scope=row] .epos-kond-vorlage-alle select")
              .FirstOrDefault();

    /// <summary>Die Namen einer Auswahlliste ohne den leeren Eintrag.</summary>
    private static List<string> Namen(IElement liste)
        => liste.QuerySelectorAll("option").Where(o => o.GetAttribute("value") != "").Select(o => o.TextContent.Trim()).ToList();

    /// <summary>
    /// Wählt in der Liste „alle Größen" den Namen. Die Rückfrage zeichnet der Reiter; ihn zeichnet in der Anwendung
    /// der Wirt über <c>Geaendert</c> neu - hier ohne Wirt von Hand, wie im Fall „aufteilen".
    /// </summary>
    private static void AlleWaehlen(IRenderedComponent<KonditionierungReiter> cut, string name)
    {
        IElement liste = ListeAlle(cut)!;
        liste.Change(liste.QuerySelectorAll("option").First(o => o.TextContent.Trim() == name).GetAttribute("value")!);
        cut.Render();
    }

    /// <summary>Die Zeilen der offenen Rückfrage.</summary>
    private static string[] Fragezeilen(IRenderedComponent<KonditionierungReiter> cut)
        => cut.Find(".epos-rueckfrage-text").TextContent.Trim().Split('\n').Select(z => z.Trim()).ToArray();

    /// <summary>Der echte Weg der Hülle ohne Datenbank mit den 14 ausgelieferten Vorlagen.</summary>
    private static KonditionierungWeg Saatweg() => KalenderkarteTests.Weg(Konditionierungsvorlagenablage.AusSaat());

    [Fact]
    public void Die_Kopfzelle_der_Zeile_Vorlage_traegt_die_Liste_alle_Groessen()
    {
        var cut = Aufbauen(KalenderkarteTests.Satz(), Saatweg());

        IElement kopf = cut.Find("table.epos-kond-matrix tr[data-zeile='vorlage'] > th[scope=row]");
        Assert.StartsWith("Vorlage", kopf.TextContent.Trim());
        IElement gruppe = kopf.QuerySelector(".epos-vorlage-wahl.epos-kond-vorlage-alle")!;
        Assert.Equal("group", gruppe.GetAttribute("role"));
        Assert.Equal("Vorlage · alle Größen", gruppe.GetAttribute("aria-label"));
        Assert.Equal("Gleichnamige Vorlage in allen Größen übernehmen: Heizen, Kühlen, Lüftung, Geräte und Personen bekommen " +
                     "die Vorlage dieses Namens, wo es sie gibt.", gruppe.GetAttribute("title"));
        Assert.Equal("alle Größen", gruppe.QuerySelector(".epos-feld-text")!.TextContent.Trim());

        IElement liste = ListeAlle(cut)!;
        Assert.Equal("Vorlage · alle Größen", liste.GetAttribute("aria-label"));
        Assert.Equal("—", liste.QuerySelector("option[value='']")!.TextContent.Trim());
        Assert.Equal("", liste.GetAttribute("value"));
        Assert.Equal(new[] { "Büro", "Schule", "Wohnen" }, Namen(liste));   // „Wohnen" fehlt nur der Lüftung
        Assert.Empty(gruppe.QuerySelectorAll(".epos-schloss"));              // ohne Wahl kein Schloss

        // Die Kopfzelle hängt an keiner Spalte: Schmal steht sie bei jeder gewählten Größe.
        cut.Find("button.epos-kond-groesse[data-groesse='4']").Click();
        Assert.NotNull(ListeAlle(cut));
        Assert.Null(cut.Find("tr[data-zeile='vorlage'] > th[scope=row]").GetAttribute("data-groesse"));
    }

    [Fact]
    public void Die_Liste_fuehrt_jeden_Namen_aus_mindestens_einer_Liste_ohne_Dubletten_in_der_Reihenfolge_des_Kerns()
    {
        var ablage = Konditionierungsvorlagenablage.AusSaat();
        Konditionierungsstand leer = Konditionierungsstand.Leer(Kalendereigentuemer.Vorlage, null);
        ablage.Hinzufuegen(Konditionierungsgroesse.Personen, "Kontor", "", DbWerte.KOND_NUTZUNG_BUERO, ausgeliefert: false,
                           leer.MitVorgabe(Konditionierungsgroesse.Personen, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(80)));
        ablage.Hinzufuegen(Konditionierungsgroesse.Heizsoll, "Aula", "", DbWerte.KOND_NUTZUNG_SCHULE, ausgeliefert: false,
                           leer.MitVorgabe(Konditionierungsgroesse.Heizsoll, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(19)));
        // „ wohnen " als eigene der Lüftung: derselbe Name wie die ausgelieferte „Wohnen" der übrigen Listen
        // (getrimmt, ohne Unterschied der Groß- und Kleinschreibung) - EIN Eintrag, mit Schloss.
        ablage.Hinzufuegen(Konditionierungsgroesse.Lueftung, " wohnen ", "", DbWerte.KOND_NUTZUNG_WOHNEN, ausgeliefert: false,
                           leer.MitVorgabe(Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_TAG, Matrixzelle.AusWert(0.4)));
        var cut = Aufbauen(KalenderkarteTests.Satz(), KalenderkarteTests.Weg(ablage));

        Assert.Equal(new[] { "Büro", "Schule", "Wohnen", "Aula", "Kontor" }, Namen(ListeAlle(cut)!));
        Assert.Equal(new[] { true, true, true, false, false }, _bearbeitung.VorlagenAlle().Select(v => v.Ausgeliefert));
        Assert.NotNull(_bearbeitung.VorlageGleichenNamens(KonditionierungGroesse.Lueftung, "WOHNEN"));
        Assert.Null(_bearbeitung.VorlageGleichenNamens(KonditionierungGroesse.Kuehlen, "Kontor"));
    }

    [Fact]
    public void Die_Liste_fehlt_im_Lesemodus_ohne_Vorlagen_ohne_Weg_auf_dem_Altweg_und_an_der_Zone()
    {
        Assert.Null(ListeAlle(Aufbauen(KalenderkarteTests.Satz(), Saatweg(), lesemodus: true)));
        Assert.Null(ListeAlle(Aufbauen(KalenderkarteTests.Satz(), KalenderkarteTests.Weg(null))));
        Assert.False(_bearbeitung.MitVorlageAlle);
        Assert.Empty(_bearbeitung.VorlagenAlle());
        Assert.Null(ListeAlle(Aufbauen(KalenderkarteTests.Satz(), Saatweg(), altweg: true)));
        Assert.Null(ListeAlle(Aufbauen(Satz(), weg: null)));

        // An einer Zone: die Karten der Zone bieten keine Vorlagen (Teilkonzept 3.4) - also keine Abkürzung.
        var arbeit = new GebaeudeArbeitsstand();
        arbeit.Laden(KalenderkarteTests.Satz(), neu: false);
        var zone = new ZoneDaten { Id = 1, Bezeichner = "Wohnen EG", Nutzflaeche = 90, Konditionierung = new KonditionierungDaten() };
        KonditionierungWeg weg = Saatweg();
        var b = new KonditionierungBearbeitung(arbeit, zone, () => weg);
        Assert.True(b.MitWeg);
        Assert.False(b.MitVorlageAlle);
        Assert.Empty(b.VorlagenAlle());
        Assert.False(b.VorlageAlleWaehlen("Büro"));
        Assert.Null(b.OffeneFrage);
        var matrix = Render<KonditionierungMatrix>(p => p.Add(x => x.Bearbeitung, b));
        Assert.NotEmpty(matrix.FindAll("tr[data-zeile='vorlage']"));
        Assert.Empty(matrix.FindAll(".epos-kond-vorlage-alle"));
    }

    /// <summary>
    /// Die EINE Rückfrage: Titel, Satz und je Größe eine Zeile; am angelegten Kalender, was ersetzt wird und was bleibt
    /// (P12); eine Größe ohne gleichnamige Vorlage bleibt; Vorgabe „Nein" - und „Nein" lässt alles.
    /// </summary>
    [Fact]
    public void Die_Wahl_stellt_eine_Rueckfrage_mit_je_Groesse_einer_Zeile()
    {
        var cut = Aufbauen(KalenderkarteTests.Satz(), Saatweg());
        Assert.True(_bearbeitung.Anlegen(KonditionierungGroesse.Heizen));
        cut.Render();
        var fassung = _bearbeitung.Daten!.Fassung;

        AlleWaehlen(cut, "Wohnen");
        Assert.Equal("Vorlage in allen Größen übernehmen", _bearbeitung.OffeneFrage!.Titel);
        Assert.True(_bearbeitung.OffeneFrage.VorgabeNein);
        string[] zeilen = Fragezeilen(cut);
        Assert.Equal("Die Vorlage „Wohnen“ in allen Größen übernehmen?", zeilen[0]);
        Assert.Equal(6, zeilen.Length);
        Assert.StartsWith("Heizen: ersetzt wird: ", zeilen[1]);
        Assert.Contains("; es bleibt: ", zeilen[1]);
        Assert.Equal("Kühlen: übernehmen", zeilen[2]);
        Assert.Equal("Lüftung: keine Vorlage dieses Namens — bleibt", zeilen[3]);
        Assert.Equal("Geräte: übernehmen", zeilen[4]);
        Assert.Equal("Personen: übernehmen", zeilen[5]);
        // Die Wahl steht mit Schloss, solange die Frage steht.
        Assert.Equal("Wohnen", ListeAlle(cut)!.QuerySelector("option[selected]")!.TextContent.Trim());
        Assert.NotNull(cut.Find(".epos-kond-vorlage-alle .epos-schloss"));

        KalenderkarteTests.Antworten(cut, ja: false);
        Assert.Null(_bearbeitung.OffeneFrage);
        Assert.Equal("", ListeAlle(cut)!.GetAttribute("value"));
        Assert.Equal(fassung, _bearbeitung.Daten!.Fassung);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
            Assert.Null(_bearbeitung.Herkunft(g));
    }

    /// <summary>
    /// Ohne „Gebäude wird gekühlt" ist „Übernehmen" der Kühlkarte gesperrt — die Abkürzung übergeht Kühlen und nennt
    /// in ihrer Zeile DENSELBEN Grund wie der Knopf der Karte.
    /// </summary>
    [Fact]
    public void Ohne_Kuehlung_nennt_die_Rueckfrage_Kuehlen_gesperrt_mit_dem_Grund_der_Karte_und_laesst_Kuehlen()
    {
        GebaeudeKatalogDaten satz = KalenderkarteTests.Satz();
        satz.KuehlungAktiv = false;
        string grund = new KonditionierungTexte().GrundKuehlenGesperrt;
        var cut = Aufbauen(satz, Saatweg(), kuehlsperre: grund);
        Assert.Equal(grund, _bearbeitung.Uebernehmensperre(KonditionierungGroesse.Kuehlen));
        Assert.Null(_bearbeitung.Uebernehmensperre(KonditionierungGroesse.Heizen));

        // Der Knopf „Übernehmen" der Kühlkarte trägt denselben Grund (die Sperre der Karte).
        KalenderkarteTests.Waehlen(cut, KonditionierungGroesse.Kuehlen, "Büro");
        Assert.Equal(grund, KalenderkarteTests.Uebernehmen(cut, KonditionierungGroesse.Kuehlen).GetAttribute("title"));
        KalenderkarteTests.Waehlen(cut, KonditionierungGroesse.Kuehlen, "keine Vorlage gewählt");

        AlleWaehlen(cut, "Büro");
        Assert.Equal("Kühlen: gesperrt — " + grund, Fragezeilen(cut)[2]);
        KalenderkarteTests.Antworten(cut, ja: true);

        Assert.Null(_bearbeitung.Herkunft(KonditionierungGroesse.Kuehlen));
        Assert.False(_bearbeitung.Angelegt(KonditionierungGroesse.Kuehlen));
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle.Where(g => g != KonditionierungGroesse.Kuehlen))
            Assert.Equal("Büro", _bearbeitung.Herkunft(g));
        Assert.Equal("Büro", _bearbeitung.HerkunftAlle());   // gemeinsam über die ungesperrten Größen
    }

    /// <summary>Die betroffenen Zonen nennt die Rückfrage EINMAL, mit Namen, aus den Befunden aller Größen.</summary>
    [Fact]
    public void Die_Rueckfrage_nennt_die_Zonen_aus_den_Befunden_aller_Groessen()
    {
        KonditionierungWeg basis = Saatweg();
        var weg = new KonditionierungWeg
        {
            ZelleSetzen = basis.ZelleSetzen, Anlegen = basis.Anlegen, Vorlagen = basis.Vorlagen,
            VorlageUebernehmen = basis.VorlageUebernehmen, LuftwechselAufteilen = basis.LuftwechselAufteilen,
            Rueckfrage = (_, o, _) => new KonditionierungRueckfrage(Array.Empty<KonditionierungPosten>(),
                                                                   Array.Empty<KonditionierungPosten>(),
                                                                   o.Groesse == KonditionierungGroesse.Personen
                                                                       ? new[] { "Büro OG", "Wohnen EG" }
                                                                       : new[] { "Wohnen EG" })
        };
        var cut = Aufbauen(KalenderkarteTests.Satz(), weg);

        AlleWaehlen(cut, "Büro");
        string[] zeilen = Fragezeilen(cut);
        Assert.Equal(7, zeilen.Length);
        Assert.Equal("Betroffene Zonen: Wohnen EG, Büro OG.", zeilen[6]);
        Assert.Equal("Heizen: übernehmen", zeilen[1]);   // ohne angelegten Kalender ersetzt die Abkürzung nichts Angelegtes
    }

    /// <summary>
    /// Der Fehler eines Schritts bricht „Ja" ab und meldet: Der Arbeitsstand bleibt der von davor — auch die Größen
    /// vor dem Fehler sind nicht übernommen, und es gibt nichts zurückzunehmen.
    /// </summary>
    [Fact]
    public void Der_Fehler_eines_Schritts_bricht_ab_und_der_Arbeitsstand_bleibt()
    {
        KonditionierungWeg basis = Saatweg();
        var weg = new KonditionierungWeg
        {
            ZelleSetzen = basis.ZelleSetzen, Anlegen = basis.Anlegen, Vorlagen = basis.Vorlagen,
            LuftwechselAufteilen = basis.LuftwechselAufteilen, Rueckfrage = basis.Rueckfrage,
            VorlageUebernehmen = (s, o, id) => o.Groesse == KonditionierungGroesse.Geraete
                ? KonditionierungErgebnis.Fehler("Probe: Geräte abgelehnt")
                : basis.VorlageUebernehmen!(s, o, id)
        };
        var cut = Aufbauen(KalenderkarteTests.Satz(), weg);
        var fassung = _bearbeitung.Daten!.Fassung;
        double? sollTag = _bearbeitung.Stand.SollTag;

        AlleWaehlen(cut, "Büro");
        KalenderkarteTests.Antworten(cut, ja: true);

        Assert.Contains("Probe: Geräte abgelehnt", _meldungen);
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            Assert.Null(_bearbeitung.Herkunft(g));
            Assert.False(_bearbeitung.Angelegt(g));
        }
        Assert.Equal(fassung, _bearbeitung.Daten!.Fassung);
        Assert.Equal(sollTag, _bearbeitung.Stand.SollTag);
        Assert.False(_bearbeitung.KannZuruecknehmen);
    }
}
