using AngleSharp.Dom;
using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Seite „Wirtschaftlichkeit" (iU9-W5.3), Vorbild
/// <c>Views/Wirtschaftlichkeit/UcWirtschaftlichkeit</c> (11 Kartenzeilen und
/// drei programmatisch gebaute Knöpfe).
///
/// <para>Soll ist die Feldkarte: vier Kennzahl-Karten, die Vergleichsgruppe
/// mit Haken, die Szenariowahl MIT dem Einstieg „Parameter…" daneben (AUFTRAG #325), der
/// Parameternachweis, die Vergleichstabelle, der Bewertungsblock nach
/// DIN EN 17463 darunter (AUFTRAG #325), die Sicht-Knöpfe (Photovoltaik, BHKW —
/// je nach Ausstattung; „Strombezug…" ist mit Q11 entfallen), „Berechnen" und der
/// Abbrechen-Knopf während eines Laufs. „Verlauf…" ist mit ETAPPE E6 entfallen: Der
/// Verlauf steht als Abschnitt in „Wie sicher ist das?" (<see cref="KapitalwertVerlaufAbschnittTests"/>).</para>
///
/// <para><b>ETAPPE E5 Teil b:</b> Karten, Szenariowahl, Nachweis, Tabelle und
/// Bewertungsblock stehen seither in den vier Abschnitten der Darstellung „Kennzahlen"
/// (Mockup Kategorie 8); die Fälle fragen nach dem Bereich, nicht nach der Stellung unter
/// der Seitenwurzel. Umschalter, Abschnitte, Karten, Bandbreite, Annahmen, Bericht und
/// ValERI-Ansicht prüft <see cref="WirtschaftlichkeitErgebnisansichtTests"/>.</para>
///
/// <para><b>Kulturpinnung</b> (Auftrag #267): Die Beschriftung des Rechenknopfs
/// im Warnband kommt seit dem Ressourcennachtrag aus <c>MyResource</c>. Der
/// CI-Läufer läuft unter <c>en-US</c>; die Hausvorrichtung
/// <see cref="EposBunitContext"/> pinnt die Oberflächenkultur auf de-DE und
/// stellt sie zurück.</para>
/// </summary>
public class WirtschaftlichkeitSeiteTests : EposBunitContext
{
    public WirtschaftlichkeitSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static ErgebnisAnsicht Ansicht(string erste = "12.500 €") => new ErgebnisAnsicht
    {
        Kacheln = new[]
        {
            new KachelZeile { Titel = "Kapitalwert ggü. Stamm", Wert = erste,
                              Quelle = "beste Variante: WP klein" },
            new KachelZeile { Titel = "Annuität", Wert = "1.200 €/a" },
            new KachelZeile { Titel = "Amortisation", Wert = "8,4 a" },
            new KachelZeile { Titel = "Interner Zinsfuß", Wert = "6,1 %" }
        },
        Matrix = new ErgebnisMatrix
        {
            Spalten = new[] { "Kennzahl", "Stamm", "WP klein" },
            Zeilen = new[]
            {
                new MatrixZeile { Titel = "Kapitalwert", Zellen = new[] { "-90.000", "-77.500" } },
                new MatrixZeile { Titel = "Annuität", Zellen = new[] { "—", "1.200" } }
            }
        }
    };

    private static WirtschaftlichkeitStand Standard(bool pv = true, bool bhkw = true)
        => new WirtschaftlichkeitStand
    {
        Varianten = new[]
        {
            new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", SimStand = "02.09.2026 10:00",
                                Speicher = "mit Speicherflotte: Lastspitzenkappung · 2 Einheiten",
                                IstStamm = true },
            new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "WP klein",
                                Projektname = "Musterhaus", SimStand = "",
                                Speicher = "ohne Stromspeicher", Auffaellig = true }
        },
        GewaehlteVarianten = new[] { 1030, 1031 },
        Szenarien = new[] { (0, "Erwartet"), (1, "Best Case"), (2, "Worst Case") },
        SzenarioId = 0,
        Parameterzeile = "Parameter: 20 a, 3,0 % · Referenz: Stammprojekt",
        Ansicht = Ansicht(),
        MitPhotovoltaik = pv,
        MitBhkw = bhkw,
        Statuszeile = "Gespeicherte Ergebnisse vom 02.09.2026 11:00."
    };

    private WirtschaftlichkeitStand _stand = Standard();
    private int _geladen;

    private IRenderedComponent<WirtschaftlichkeitSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<WirtschaftlichkeitSeite>>? mehr = null,
        WirtschaftlichkeitStand? stand = null)
    {
        _stand = stand ?? Standard();
        _geladen = 0;
        return Render<WirtschaftlichkeitSeite>(p =>
        {
            p.Add(x => x.Laden, () => { _geladen++; return _stand; });
            mehr?.Invoke(p);
        });
    }

    /// <summary>Ein Parametersatz, der jeden Unterdialog aufbauen lässt.</summary>
    private static IReadOnlyDictionary<string, object> LeererSatz()
        => new Dictionary<string, object>();

    private static IReadOnlyList<IElement> Fussknoepfe(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-seite > .epos-leiste button");

    /// <summary>
    /// AUFTRAG #325 (Anwenderwunsch 17.09.2026): Der Einstieg „Parameter…" steht
    /// nicht mehr in der Fussleiste, sondern OBEN in der Zeile der Szenariowahl. Die
    /// Prüffälle fragen deshalb nach dem BEREICH und nicht nach einer Stellung in
    /// einer Leiste — sonst verschiebt der nächste Umbau wieder acht Indizes.
    /// </summary>
    private static IElement Einstieg(IRenderedComponent<WirtschaftlichkeitSeite> cut,
                                     WirtschaftlichkeitSeite.Unterdialog art)
        => art == WirtschaftlichkeitSeite.Unterdialog.Parameter
            ? cut.Find(".epos-seite-zeile button")
            : Fussknoepfe(cut)[art == WirtschaftlichkeitSeite.Unterdialog.Photovoltaik ? 0 : 1];

    // =====================================================================
    // Feldbestand (Feldkarte)
    // =====================================================================

    [Fact]
    public void Die_Seite_zeigt_Karten_Liste_Szenario_Parameterzeile_und_Tabelle()
    {
        var cut = Zeige();

        Assert.Equal(4, cut.FindAll(".epos-kennzahlkachel").Count);
        // Wahlspalte + Art, Bezeichner, Projektname, Stromspeicher (VF-1), Simulation
        Assert.Equal(6, cut.FindAll(".epos-raster:not(.epos-matrix) thead th").Count);
        Assert.Equal(2, cut.FindAll(".epos-raster tbody input[type=checkbox]").Count);
        Assert.Single(cut.FindAll("select"));                          // Szenario
        // ETAPPE E6: Der Abschnitt „Verlauf" trägt eigene Herleitungszeilen — gesucht wird
        // die Parameterzeile, nicht die erste Zeile der Seite.
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent.Contains("Referenz: Stammprojekt"));
        Assert.Single(cut.FindAll(".epos-matrix"));
    }

    /// <summary>
    /// AUFTRAG VF-1 (Anwenderbefund 17.09.2026): <b>Jede Zeile der Vergleichsgruppe sagt,
    /// womit sie ihren Stromspeicher rechnet.</b>
    ///
    /// <para>Bis hierher stand da nichts: Eine Variante ohne Speicher sah aus wie eine
    /// mit, und der Anwender las zwei Zahlen gegeneinander, die verschiedene Anlagen
    /// meinten. Stamm und Varianten DÜRFEN verschiedene Flotten führen — die Spalte ist
    /// die Antwort darauf, kein Hinweis auf einen Fehler.</para>
    /// </summary>
    [Fact]
    public void Die_Vergleichsgruppe_nennt_je_Zeile_den_Speicherkontext()
    {
        var cut = Zeige();

        var koepfe = cut.FindAll(".epos-raster:not(.epos-matrix) thead th")
                        .Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Stromspeicher", koepfe);

        string tabelle = cut.Find(".epos-raster:not(.epos-matrix)").TextContent;
        Assert.Contains("mit Speicherflotte: Lastspitzenkappung · 2 Einheiten", tabelle);
        Assert.Contains("ohne Stromspeicher", tabelle);
    }

    /// <summary>
    /// GEGENPROBE zur Spalte: Ohne lesbaren Kontext bleibt die Zelle leer — die Seite
    /// erfindet nichts und fällt nicht aus (VF-1).
    /// </summary>
    [Fact]
    public void Ohne_Speicherkontext_bleibt_die_Zelle_leer()
    {
        WirtschaftlichkeitStand stand = Standard();
        foreach (VarianteZeile z in stand.Varianten) z.Speicher = "";
        var cut = Zeige(stand: stand);

        Assert.Contains("Stromspeicher",
            cut.FindAll(".epos-raster:not(.epos-matrix) thead th").Select(e => e.TextContent.Trim()));
        Assert.DoesNotContain("ohne Stromspeicher",
            cut.Find(".epos-raster:not(.epos-matrix)").TextContent);
    }

    /// <summary>
    /// AUFTRAG VF-1, Teil C: <b>Nimmt der Sammler gespeicherte Läufe, sagt es die
    /// Parameterzeile.</b> Die Hülle hängt den Satz an; die Seite zeigt ihn an derselben
    /// Stelle wie den übrigen Parameternachweis — ein zweiter Ort wäre ein zweiter
    /// Nachweis.
    /// </summary>
    [Fact]
    public void Die_Parameterzeile_traegt_den_Hinweis_auf_gespeicherte_Laeufe()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Parameterzeile += " · Ergebnisse aus gespeicherten Läufen vom 02.09.2026 10:00; " +
                                "nicht neu gerechnet";
        var cut = Zeige(stand: stand);

        string zeile = cut.FindAll(".epos-herleitung-text")
                          .Select(e => e.TextContent)
                          .First(t => t.StartsWith("Parameter:"));
        Assert.Contains("nicht neu gerechnet", zeile);
    }

    /// <summary>
    /// ETAPPE W5‑B‑9 (09.09.2026): Über dem Parameternachweis steht die Statuszeile des
    /// GEWÄHLTEN Szenarios — mit welchem Satz es rechnet und ob der aus Vorgaben oder
    /// gepflegten Werten besteht. Sie hängt an der Ansicht und wechselt deshalb mit der
    /// Szenariowahl mit.
    /// </summary>
    [Fact]
    public void Die_Szenariozeile_steht_ueber_dem_Parameternachweis()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Ansicht.Szenariozeile = "Szenario: Best: Vorgaben — i = 2,0 %";
        var cut = Zeige(stand: stand);

        var zeilen = cut.FindAll(".epos-herleitung-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Szenario: Best: Vorgaben — i = 2,0 %", zeilen);
        Assert.True(zeilen.IndexOf("Szenario: Best: Vorgaben — i = 2,0 %") <
                    zeilen.FindIndex(t => t.StartsWith("Parameter:")));
    }

    /// <summary>
    /// Eine LEERE Szenariozeile wird gar nicht erst gezeichnet — eine leere
    /// Herleitungszeile ist kein Hinweis, sondern eine Lücke. (Der Fall
    /// „Karten, Liste, Szenario, Parameterzeile“ oben hängt daran: Er greift die
    /// ERSTE Herleitungszeile.)
    /// </summary>
    [Fact]
    public void Eine_leere_Szenariozeile_wird_nicht_gezeichnet()
    {
        var cut = Zeige();

        Assert.Equal("", _stand.Ansicht.Szenariozeile);
        Assert.DoesNotContain(cut.FindAll(".epos-herleitung-text"),
                              e => string.IsNullOrWhiteSpace(e.TextContent));
    }

    /// <summary>
    /// ETAPPE W5‑B‑11 (09.09.2026, VALERI-Lücken G7 / G10 / G1,G3,G5): Unter dem
    /// Parameternachweis stehen zwei weitere Herleitungszeilen — der
    /// Betrachtungszeitraum gegen die Nutzungsdauern und die offengelegten
    /// Vereinfachungen samt der Herkunft von Eigenverbrauchsquote und Einspeiseanteil.
    /// </summary>
    [Fact]
    public void Der_Nachweisblock_zeigt_Zeitraum_und_Vereinfachungen()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Zeitraumzeile = "Betrachtungszeitraum T = 20 a · Nutzungsdauern 15 bis 25 a";
        stand.Vereinfachungszeile = "Vereinfachungen (VALERI): kein Endjahr je Kostenposition.";
        var cut = Zeige(stand: stand);

        var zeilen = cut.FindAll(".epos-herleitung-text").Select(e => e.TextContent).ToList();
        Assert.Contains(zeilen, z => z.StartsWith("Betrachtungszeitraum T = 20 a"));
        Assert.Contains(zeilen, z => z.StartsWith("Vereinfachungen (VALERI)"));

        // Sie stehen UNTER dem Parameternachweis — erst die Parameter, dann ihre
        // Einordnung.
        int parameter = zeilen.FindIndex(z => z.StartsWith("Parameter:"));
        Assert.True(parameter < zeilen.FindIndex(z => z.StartsWith("Betrachtungszeitraum")));
        Assert.True(parameter < zeilen.FindIndex(z => z.StartsWith("Vereinfachungen")));
    }

    /// <summary>
    /// ETAPPE W5‑B‑11 (VALERI-Lücke G9): der Vorschlag zur Entscheidung. ETAPPE E5 Teil b
    /// (Mockup „Lohnt es sich?"): Er steht im ERSTEN Abschnitt unter den Karten, aus
    /// deren Urteilen er entsteht — und damit VOR der Gliederung, nicht mehr unter einer
    /// langen Vergleichstabelle.
    /// </summary>
    [Fact]
    public void Die_Empfehlungszeile_steht_im_Abschnitt_Lohnt_es_sich()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Ansicht.Empfehlungszeile =
            "Vorschlag zur Entscheidung: Variante „WP klein“ — " +
            "Kapitalwertdifferenz zum Stammprojekt +12.300 € (Erwartet).";
        var cut = Zeige(stand: stand);

        IElement lohnt = cut.FindAll("section.epos-gruppenkopf")[0];
        Assert.Contains("Vorschlag zur Entscheidung:", lohnt.TextContent);

        // Reihenfolge im gezeichneten Baum: erst der Satz, dann die Gliederung.
        var knoten = cut.FindAll(".epos-wirt-gliederung, .epos-herleitung-text");
        int gliederung = knoten.ToList().FindIndex(e => e.ClassList.Contains("epos-wirt-gliederung"));
        int satz = knoten.ToList().FindIndex(
            e => e.TextContent.StartsWith("Vorschlag zur Entscheidung:"));
        Assert.True(satz >= 0 && gliederung > satz);
    }

    /// <summary>
    /// Ohne Variante mit Erwartet-Ergebnis bleibt der Text leer — dann wird die Zeile
    /// gar nicht erst gezeichnet (dieselbe Regel wie bei der Szenariozeile: eine leere
    /// Herleitungszeile ist kein Hinweis, sondern eine Lücke).
    /// </summary>
    [Fact]
    public void Eine_leere_Empfehlungszeile_wird_nicht_gezeichnet()
    {
        var cut = Zeige();

        Assert.Equal("", _stand.Ansicht.Empfehlungszeile);
        Assert.DoesNotContain(cut.FindAll(".epos-herleitung-text"),
                              e => e.TextContent.StartsWith("Vorschlag zur Entscheidung:"));
    }

    [Fact]
    public void Die_Matrix_traegt_je_Version_eine_Spalte()
    {
        var cut = Zeige();

        var koepfe = cut.Find(".epos-matrix thead tr").QuerySelectorAll("th");
        Assert.Equal(3, koepfe.Length);
        Assert.Equal("Kennzahl", koepfe[0].TextContent);
        Assert.Equal("WP klein", koepfe[2].TextContent);

        var zeilen = cut.Find(".epos-matrix tbody").QuerySelectorAll("tr");
        Assert.Equal(2, zeilen.Length);
        Assert.Equal("Kapitalwert", zeilen[0].QuerySelector(".epos-matrix-titel")!.TextContent);
        Assert.Equal("-77.500", zeilen[0].QuerySelectorAll(".epos-matrix-zelle")[1].TextContent);
    }

    [Fact]
    public void Ohne_Ergebnisse_bleibt_die_Tabelle_weg()
    {
        WirtschaftlichkeitStand ohne = Standard();
        ohne.Ansicht = new ErgebnisAnsicht { Kacheln = ohne.Ansicht.Kacheln };
        var cut = Zeige(stand: ohne);

        Assert.Empty(cut.FindAll(".epos-matrix"));
        Assert.Equal(4, cut.FindAll(".epos-kennzahlkachel").Count);
    }

    /// <summary>
    /// AUFTRAG #325: „Parameter…" ist aus der Fussleiste heraus — dort stehen noch
    /// PV, BHKW und Berechnen. Der Einstieg selbst ist nicht
    /// verschwunden, er steht in der Zeile der Szenariowahl.
    ///
    /// <para><b>ETAPPE E6 (K8, U2):</b> „Verlauf…" ist entfallen; <b>Q11 (E7b):</b>
    /// „Strombezug…" ebenso — die Leiste trägt höchstens DREI Knöpfe; der Verlauf
    /// steht als Abschnitt in „Wie sicher ist das?".</para>
    /// </summary>
    [Fact]
    public void Die_zwei_Sichtknoepfe_folgen_der_Ausstattung()
    {
        var alle = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));
        Assert.Equal(3, Fussknoepfe(alle).Count);   // PV, BHKW, Berechnen

        var ohne = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()),
                         stand: Standard(pv: false, bhkw: false));
        Assert.Equal(1, Fussknoepfe(ohne).Count);   // Berechnen
    }

    /// <summary>
    /// AUFTRAG #325 (Anwenderwunsch 17.09.2026): „button parameter nach oben … auf
    /// Ebene Szenario". Der Knopf steht in DERSELBEN Zeile wie die Szenariowahl —
    /// nicht mehr unter der langen Vergleichstabelle, wo ihn niemand fand —, trägt
    /// dieselbe Beschriftung und öffnet denselben Bereich.
    ///
    /// <para>Kein Delegat, kein Knopf: Ohne Gaben steht er gar nicht da.</para>
    /// </summary>
    [Fact]
    public void Der_Parameter_Einstieg_steht_in_der_Zeile_der_Szenariowahl()
    {
        WirtschaftlichkeitSeite.Unterdialog gefragt = WirtschaftlichkeitSeite.Unterdialog.Keins;
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) =>
        {
            gefragt = a;
            return LeererSatz();
        }));

        // Die Zeile trägt das Auswahlfeld UND den Knopf. ETAPPE E6: Auch die Bedienleiste
        // des Verlaufs ist eine .epos-seite-zeile — gemeint ist die der Szenariowahl.
        IElement zeile = cut.Find(".epos-wirt-szenariozeile");
        Assert.Single(zeile.QuerySelectorAll("select"));
        IElement knopf = zeile.QuerySelector("button")!;
        Assert.Equal("Parameter…", knopf.TextContent.Trim());

        // Und er steht NICHT mehr in der Fussleiste.
        Assert.DoesNotContain(Fussknoepfe(cut), k => k.TextContent.Trim() == "Parameter…");

        knopf.Click();
        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Parameter, gefragt);
        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Parameter, cut.Instance.OffenerUnterdialog);
    }

    /// <summary>Ohne Gaben kein Knopf — auch nicht oben (A-18 aus Welle 2).</summary>
    [Fact]
    public void Ohne_Gaben_steht_auch_oben_kein_Parameter_Knopf()
    {
        var cut = Zeige();

        Assert.Empty(cut.Find(".epos-seite-zeile").QuerySelectorAll("button"));
    }

    /// <summary>
    /// Q11 (E7b, Anwender 22.09.2026: „kein HT/NT"): Den Einstieg „Strombezug…" gibt
    /// es nicht mehr. Er pflegte die Einkaufsseite des Tarifsatzes — Zonenpreise und
    /// die zweistufige Leistungspreis-Staffel. Den Zeitzonentarif gibt es nicht mehr,
    /// die Staffel pflegt der Stromträger in der Kostenverwaltung.
    /// </summary>
    [Fact]
    public void Den_Strombezug_Einstieg_gibt_es_nicht_mehr()
    {
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Assert.DoesNotContain(Fussknoepfe(cut), k => k.TextContent.Trim() == "Strombezug…");
        Assert.DoesNotContain("Strombezug…", cut.Markup);
    }

    /// <summary>Ohne Delegat kein Knopf (A-18 aus Welle 2).</summary>
    [Fact]
    public void Ohne_Gaben_bleibt_nur_Berechnen()
    {
        var cut = Zeige();

        Assert.Single(Fussknoepfe(cut));
        Assert.Equal("Berechnen", Fussknoepfe(cut)[0].TextContent.Trim());
    }

    /// <summary>Ä16: Der Sammel-Einstieg „Tarifstruktur…" gibt es nicht mehr.</summary>
    [Fact]
    public void Der_Sammel_Einstieg_Tarifstruktur_fehlt()
    {
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Assert.DoesNotContain("Tarifstruktur", cut.Markup);
    }

    // =====================================================================
    // Vorbelegung und Auswahl
    // =====================================================================

    [Fact]
    public void Die_Seite_laedt_beim_Aufbau_und_zeigt_die_Statuszeile()
    {
        var cut = Zeige();

        Assert.Equal(1, _geladen);
        Assert.Contains("Gespeicherte Ergebnisse vom", cut.Instance.Status);
    }

    [Fact]
    public void Der_Haken_der_Stammzeile_ist_gesperrt()
    {
        var cut = Zeige();

        var haken = cut.FindAll(".epos-raster tbody input[type=checkbox]");
        Assert.True(haken[0].HasAttribute("disabled"));
        Assert.False(haken[1].HasAttribute("disabled"));
    }

    /// <summary>
    /// Ein Szenariowechsel zeigt neu, OHNE zu rechnen und ohne neu zu laden. ETAPPE E5
    /// Teil b (U4): <b>Die Klappliste steuert nur die Tafeln darunter</b> — die
    /// Gliederung zeigt das gewählte Szenario, die Karten darüber bleiben stehen (sie
    /// zeigen den Erwartungsfall).
    /// </summary>
    [Fact]
    public void Ein_Szenariowechsel_zeigt_neu_ohne_zu_rechnen()
    {
        int gefragt = -1;
        var cut = Zeige(p => p.Add(x => x.Anzeigen, (int id) =>
        {
            gefragt = id;
            ErgebnisAnsicht a = Ansicht("9.000 €");
            a.Matrix = new ErgebnisMatrix
            {
                Spalten = new[] { "Kennzahl", "Stamm", "WP klein" },
                Zeilen = new[] { new MatrixZeile { Titel = "Kapitalwert", Zellen = new[] { "-91.000", "-80.000" } } }
            };
            return a;
        }));

        cut.Find("select").Change("2");

        Assert.Equal(2, gefragt);
        Assert.Equal("-80.000",
                     cut.Find(".epos-wirt-gliederung tbody tr").QuerySelectorAll(".epos-matrix-zelle")[1].TextContent);
        Assert.Equal("12.500 €", cut.FindAll(".epos-kennzahlkachel-wert")[0].TextContent);
        Assert.Equal(1, _geladen);   // NICHT neu geladen
    }

    // =====================================================================
    // AUFTRAG #325 — Bewertung nach DIN EN 17463 auf der Seite
    // =====================================================================

    /// <summary>
    /// <b>Der Block steht unter der Kennzahltabelle und ist zugeklappt.</b>
    /// Anwenderentscheid 17.09.2026: „heraus nehmen aus Parameter Dialog: 1.
    /// Bewertung nach DIN EN 17463" — in die Wirtschaftlichkeitsseite, weil die Wirkungen
    /// dort stehen, wo auch der Kapitalwert steht. ETAPPE E17: aufgeklappt trägt er die
    /// LISTE der Wirkungen statt des Freitextfelds.
    ///
    /// <para>Kein Delegat, kein Knopf: Ohne Schreibweg gibt es den Block nicht.</para>
    /// </summary>
    [Fact]
    public void Der_Bewertungsblock_steht_unter_der_Tabelle_und_haengt_am_Schreibweg()
    {
        var ohne = Zeige();
        Assert.Empty(ohne.FindAll("button.epos-modulparameter-knopf"));

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => true));

        IElement knopf = cut.Find("button.epos-modulparameter-knopf");
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Contains("Bewertung nach DIN EN 17463", knopf.TextContent);

        // Zugeklappt steht die Liste nicht da.
        Assert.Empty(cut.FindAll(".epos-wirt-wirkungen"));
        Assert.False(cut.Instance.BewertungOffen);

        knopf.Click();

        Assert.Equal("true", cut.Find("button.epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll(".epos-wirt-wirkungen"));
        Assert.Empty(cut.FindAll("textarea"));   // der Freitext ist keine Eingabe mehr
        Assert.True(cut.Instance.BewertungOffen);
    }

    /// <summary>
    /// ETAPPE E17 (V‑G11): <b>Der Block zeigt die gepflegten Wirkungen und schreibt die Liste
    /// auf Zuruf fort</b> — Beschreibung, Kategorie, Dauer und Wirkungsgrade als Wahlfelder,
    /// geschrieben erst mit „Speichern", und die Beurteilung folgt der Wahl sofort.
    /// </summary>
    [Fact]
    public void Der_Bewertungsblock_zeigt_die_Wirkungen_und_schreibt_die_Liste_fort()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Wirkungen = new List<ProjektWirkung>
        {
            new() { Kategorie = NichtMonetaereWirkungen.SONSTIG, Beschreibung = "Versorgungssicherheit" }
        };

        IReadOnlyList<ProjektWirkung>? geschrieben = null;
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> l) =>
        {
            geschrieben = l.Select(w => w.Kopie()).ToList();
            return true;
        }), stand: stand);

        cut.Find("button.epos-modulparameter-knopf").Click();
        Assert.Equal("Versorgungssicherheit",
                     cut.Find(".epos-wirt-wirkungen input.epos-eingabe").GetAttribute("value"));
        Assert.Contains("nicht beurteilt", cut.Find(".epos-wirt-wirkungen-beurteilung").TextContent);

        // Dauer lang (3), Umwelt mittel (2): Beurteilung 3 × 2 = 6.
        IReadOnlyList<IElement> wahl = cut.FindAll(".epos-wirt-wirkungen select");
        Assert.Equal(5, wahl.Count);    // Kategorie, Dauer, Organisation, Mitarbeiter, Umwelt
        wahl[1].Change("3");
        cut.FindAll(".epos-wirt-wirkungen select")[4].Change("2");
        cut.FindAll(".epos-wirt-wirkungen select")[0].Change("0");   // Energiefluss
        Assert.Equal("6 von 9", cut.Find(".epos-wirt-wirkungen-beurteilung").TextContent.Trim());

        // Bis zum Knopfdruck ist nichts geschrieben.
        Assert.Null(geschrieben);

        Speichernknopf(cut).Click();

        Assert.NotNull(geschrieben);
        ProjektWirkung w = Assert.Single(geschrieben!);
        Assert.Equal(NichtMonetaereWirkungen.ENERGIEFLUSS, w.Kategorie);
        Assert.Equal(3, w.Dauer);
        Assert.Equal(2, w.WirkungUmwelt);
        Assert.Null(w.WirkungOrganisation);
        Assert.Equal(6, w.Beurteilung);
        Assert.Contains("gespeichert", cut.Instance.Status);

        // Nach dem Schreiben trägt der Arbeitsstand die geschriebene Liste.
        Assert.Equal(3, cut.Instance.Wirkungen[0].Dauer);
    }

    /// <summary>
    /// ETAPPE E17: <b>Zeilen hinzufügen und entfernen</b> — „Wirkung hinzufügen" hängt eine
    /// leere Zeile „sonstig" an, das Kreuz nimmt genau ihre Zeile weg.
    /// </summary>
    [Fact]
    public void Wirkungen_lassen_sich_hinzufuegen_und_entfernen()
    {
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => true));
        cut.Find("button.epos-modulparameter-knopf").Click();

        Assert.Single(cut.FindAll(".epos-wirt-wirkungen-leer"));
        cut.Find(".epos-wirt-wirkungen-neu").Click();
        cut.Find(".epos-wirt-wirkungen-neu").Click();

        Assert.Equal(2, cut.Instance.Wirkungen.Count);
        Assert.All(cut.Instance.Wirkungen, w => Assert.Equal(NichtMonetaereWirkungen.SONSTIG, w.Kategorie));
        Assert.Equal(2, cut.FindAll(".epos-wirt-wirkungen tbody tr").Count);

        cut.FindAll(".epos-wirt-wirkungen input.epos-eingabe")[1].Input("Komfort");
        cut.FindAll(".epos-wirt-wirkungen-loeschen")[0].Click();

        ProjektWirkung rest = Assert.Single(cut.Instance.Wirkungen);
        Assert.Equal("Komfort", rest.Beschreibung);
    }

    /// <summary>
    /// <b>Nach erneutem Laden stehen die Wirkungen wieder da.</b> Der Prüffall geht den
    /// ganzen Weg: eingeben, speichern, den Stand neu aus der Quelle lesen lassen (wie nach
    /// einem Seitenwechsel) und wieder aufklappen.
    /// </summary>
    [Fact]
    public void Gespeicherte_Wirkungen_stehen_nach_erneutem_Laden_wieder_da()
    {
        WirtschaftlichkeitStand stand = Standard();
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> l) =>
        {
            // Die Hülle schreibt und liefert die Liste beim nächsten Laden zurück.
            stand.Wirkungen = l.Select(w => w.Kopie()).ToList();
            return true;
        }), stand: stand);

        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.Find(".epos-wirt-wirkungen-neu").Click();
        cut.Find(".epos-wirt-wirkungen input.epos-eingabe").Input("Erfüllung einer Auflage");
        Speichernknopf(cut).Click();

        // Zuklappen und die Seite frisch laden lassen — wie nach einem Seitenwechsel.
        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.InvokeAsync(() => cut.Instance.Auffrischen());
        cut.Find("button.epos-modulparameter-knopf").Click();

        Assert.Equal("Erfüllung einer Auflage",
                     cut.Find(".epos-wirt-wirkungen input.epos-eingabe").GetAttribute("value"));
        Assert.Equal("Erfüllung einer Auflage", cut.Instance.NichtMonetaer);
    }

    /// <summary>
    /// <b>Ein gescheitertes Schreiben SAGT es und behält die Eingabe.</b> Ein stiller
    /// Fehlschlag wäre die Behauptung, die Liste stünde in der Datenbank.
    /// </summary>
    [Fact]
    public void Ein_gescheitertes_Schreiben_meldet_und_behaelt_die_Eingabe()
    {
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => false));

        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.Find(".epos-wirt-wirkungen-neu").Click();
        cut.Find(".epos-wirt-wirkungen input.epos-eingabe").Input("Komfort");
        Speichernknopf(cut).Click();

        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.Equal("Komfort", cut.Instance.NichtMonetaer);
    }

    /// <summary>
    /// <b>Der Vermerk am Knopf</b> (Speichervermerk): „Gespeichert um …" steht neben
    /// „Speichern"; bis zur nächsten Eingabe ist der Knopf weich gesperrt und nennt beim
    /// Klick den Grund, statt ein zweites Mal zu schreiben. Die Eingabe nimmt den Vermerk
    /// zurück.
    /// </summary>
    [Fact]
    public void Bewertung_speichern_meldet_am_Knopf_und_die_Eingabe_nimmt_den_Vermerk_zurueck()
    {
        int schreibvorgaenge = 0;
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern,
                                   (IReadOnlyList<ProjektWirkung> _) => { schreibvorgaenge++; return true; }));

        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.Find(".epos-wirt-wirkungen-neu").Click();
        cut.Find(".epos-wirt-wirkungen input.epos-eingabe").Input("Komfort");
        Speichernknopf(cut).Click();

        Assert.Equal(1, schreibvorgaenge);
        Assert.StartsWith("Gespeichert um ", cut.Find(".epos-speichervermerk [role=status]").TextContent);
        Assert.Equal("true", Speichernknopf(cut).GetAttribute("aria-disabled"));

        Speichernknopf(cut).Click();
        Assert.Equal(1, schreibvorgaenge);
        Assert.Equal("Keine Änderung — es gibt nichts zu speichern.", cut.Find(".epos-speichervermerk [role=status]").TextContent);

        cut.Find(".epos-wirt-wirkungen input.epos-eingabe").Input("Komfort und Ruhe");
        Assert.Empty(cut.FindAll(".epos-speichervermerk [role=status]"));
        Assert.False(Speichernknopf(cut).HasAttribute("aria-disabled"));
    }

    /// <summary>Ein gescheitertes Schreiben steht zusätzlich rot am Knopf.</summary>
    [Fact]
    public void Ein_gescheitertes_Schreiben_steht_rot_am_Knopf()
    {
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => false));

        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.Find(".epos-wirt-wirkungen-neu").Click();
        cut.Find(".epos-wirt-wirkungen input.epos-eingabe").Input("Komfort");
        Speichernknopf(cut).Click();

        var vermerk = cut.Find(".epos-speichervermerk [role=status]");
        Assert.Contains("epos-status--fehler", vermerk.ClassName);
        Assert.NotEmpty(vermerk.TextContent.Trim());
        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.False(Speichernknopf(cut).HasAttribute("aria-disabled"));
    }

    /// <summary>
    /// ETAPPE E13 (E7c3‑Q6 a): Scheitert das Schreiben, nennt die Statuszeile den
    /// Speicherfehler des Kerns — einmal, mit dem Text des Kerns; das Band darüber sagt
    /// weiter, DASS nicht gespeichert wurde.
    /// </summary>
    [Fact]
    public void Ein_gescheitertes_Schreiben_nennt_den_Speicherfehler_in_der_Statuszeile()
    {
        const string zeile = "Speichern gescheitert: SqliteException: SQLite Error 19: 'E13-Probe'.";
        var cut = Zeige(p => p
            .Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => false)
            .Add(x => x.Speicherfehlerzeile, () => zeile));

        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.Find(".epos-wirt-wirkungen-neu").Click();
        cut.Find(".epos-wirt-wirkungen input.epos-eingabe").Input("Komfort");
        Speichernknopf(cut).Click();

        Assert.Equal(zeile, cut.Instance.Status);
        Assert.Single(cut.FindAll(".epos-status"), e => e.TextContent.Contains("E13-Probe", StringComparison.Ordinal));
        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.DoesNotContain("E13-Probe", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// ETAPPE E13 (E7c3‑Q6 a): Was die Hülle an Lade-, Speicher- und Vorsorgegründen in
    /// die Statuszeile schreibt, zeigt die Seite genau so — ohne Warnzeichen, also ohne
    /// den Anstoß „neu berechnen", der für einen Lesefehler nicht stimmt.
    /// </summary>
    [Fact]
    public void Die_Statuszeile_zeigt_die_Gruende_des_Kerns()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Statuszeile = "Gespeicherte Ergebnisse vom 24.09.2026 17:20. "
            + "Gespeicherte Ergebnisse nicht vollständig gelesen: FormatException: kaputt "
            + "Tabellenvorsorge unvollständig: SqliteException: database is locked";
        var cut = Zeige(stand: stand);

        Assert.Equal(stand.Statuszeile, cut.Instance.Status);
        Assert.Single(cut.FindAll(".epos-status"), e => e.TextContent.Contains("FormatException: kaputt", StringComparison.Ordinal));
        Assert.DoesNotContain(ErgebnisMatrix.WARN_PRAEFIX.Trim(), cut.Instance.Status);
    }

    /// <summary>
    /// <b>AUFTRAG #328: Die Wirkungen stehen je Zustand an genau EINER Stelle.</b>
    /// Zugeklappt weist der Kopf des Blocks sie aus (ETAPPE E17: die Beschreibungen der
    /// Liste, mit „; " verbunden) — mit dem vollen Text im <c>title</c>. Aufgeklappt trägt
    /// der Kopf nur seinen Titel, weil die Wirkungen dann in der Liste darunter stehen.
    /// </summary>
    [Fact]
    public void Der_Ausweis_steht_im_Kopf_und_weicht_der_offenen_Liste()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Wirkungen = new List<ProjektWirkung>
        {
            new() { Beschreibung = "Netzstabilität" },
            new() { Beschreibung = "Imagegewinn", Kategorie = NichtMonetaereWirkungen.FINANZIELL }
        };

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => true), stand: stand);

        // ZUGEKLAPPT: Titel und Ausweis nebeneinander, voller Text im Tooltip.
        IElement knopf = cut.Find("button.epos-modulparameter-knopf");
        Assert.Contains("Bewertung nach DIN EN 17463", knopf.TextContent);
        Assert.Equal("Netzstabilität; Imagegewinn", cut.Find(".epos-modulparameter-ausweis").TextContent);
        Assert.Equal("Netzstabilität; Imagegewinn", knopf.GetAttribute("title"));

        // OFFEN: nur der Titel im Kopf, die Wirkungen in der Liste.
        knopf.Click();

        Assert.Empty(cut.FindAll(".epos-modulparameter-ausweis"));
        Assert.Null(cut.Find("button.epos-modulparameter-knopf").GetAttribute("title"));
        Assert.Equal(2, cut.FindAll(".epos-wirt-wirkungen tbody tr").Count);
    }

    /// <summary>
    /// <b>AUFTRAG #328: Leer bleibt leer.</b> Ohne gepflegte Wirkung trägt der Kopf nur
    /// seinen Titel — kein „—", kein leeres <c>span</c> und kein leerer Tooltip. Eine Zeile
    /// ohne Beschreibung ist nichts Erfasstes.
    /// </summary>
    [Fact]
    public void Ohne_gepflegte_Wirkung_traegt_der_Kopf_nur_seinen_Titel()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Wirkungen = new List<ProjektWirkung> { new() { Beschreibung = "   " } };

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => true), stand: stand);

        IElement knopf = cut.Find("button.epos-modulparameter-knopf");
        Assert.Empty(cut.FindAll(".epos-modulparameter-ausweis"));
        Assert.Null(knopf.GetAttribute("title"));

        // Im Kopf stehen genau zwei Kinder: der Pfeil und der Titel.
        Assert.Equal(2, knopf.Children.Length);
        Assert.Contains("Bewertung nach DIN EN 17463", knopf.TextContent);
    }

    /// <summary>
    /// ETAPPE E17 (E17‑Q3 a): <b>Das Altfeld bleibt lesbar und ist gekennzeichnet</b> — der
    /// Freitext steht aufgeklappt als Herleitungszeile „Altfeld …", ohne Eingabe; ohne Text
    /// fehlt die Zeile.
    /// </summary>
    [Fact]
    public void Das_Altfeld_steht_gekennzeichnet_und_nur_lesbar_da()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.NichtMonetaer = "Versorgungssicherheit, Arbeitsschutz";

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => true), stand: stand);
        cut.Find("button.epos-modulparameter-knopf").Click();

        string[] herleitungen = cut.FindAll("p.epos-herleitung").Select(e => e.TextContent).ToArray();
        Assert.Contains(herleitungen, t => t.StartsWith("Altfeld", StringComparison.Ordinal)
                                            && t.Contains("Versorgungssicherheit, Arbeitsschutz"));
        Assert.Empty(cut.FindAll("textarea"));

        var ohne = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => true));
        ohne.Find("button.epos-modulparameter-knopf").Click();
        Assert.DoesNotContain(ohne.FindAll("p.epos-herleitung"), e => e.TextContent.StartsWith("Altfeld", StringComparison.Ordinal));
    }

    /// <summary>
    /// <b>AUFTRAG #328: Im Nachweisblock stehen die Wirkungen nicht.</b> Er hatte dort
    /// seine fertig formulierte Zeile („Nicht monetäre Wirkungen: …"); die ist entfallen,
    /// damit derselbe Satz nicht zweimal auf einem Bildschirm steht. Die übrigen
    /// Nachweiszeilen bleiben unberührt.
    /// </summary>
    [Fact]
    public void Der_Nachweisblock_zeigt_die_Wirkungszeile_nicht_mehr()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Wirkungen = new List<ProjektWirkung> { new() { Beschreibung = "Netzstabilität, Imagegewinn" } };
        stand.Vereinfachungszeile = "Vereinfachungen: pauschal.";

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (IReadOnlyList<ProjektWirkung> _) => true), stand: stand);

        string[] herleitungen = cut.FindAll("p.epos-herleitung")
                                   .Select(e => e.TextContent).ToArray();

        Assert.Contains(herleitungen, t => t.Contains("Vereinfachungen"));
        Assert.DoesNotContain(herleitungen, t => t.Contains("Netzstabilität"));
    }

    /// <summary>Der Speichernknopf des Bewertungsblocks — er steht ohne Leiste da,
    /// damit die Seite genau EINE Fussleiste behält. ETAPPE E5 Teil b: Der Block steht
    /// seither im Abschnitt „Was ist angenommen?", der Knopf also nicht mehr unmittelbar
    /// unter der Seitenwurzel — gesucht wird er dort, wo er steht, und nie in einer
    /// Leiste.</summary>
    private static IElement Speichernknopf(IRenderedComponent<WirtschaftlichkeitSeite> cut)
    {
        IElement knopf = cut.FindAll(".epos-gruppenkopf-koerper button.epos-knopf")
                            .First(k => k.TextContent.Trim() == "Speichern");
        Assert.Null(knopf.Closest(".epos-leiste"));
        return knopf;
    }

    // =====================================================================
    // Unterdialoge in der Überlagerung
    // =====================================================================

    [Theory]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    public void Jeder_Sichtknopf_oeffnet_seinen_Bereich(
        WirtschaftlichkeitSeite.Unterdialog erwartet)
    {
        WirtschaftlichkeitSeite.Unterdialog gefragt = WirtschaftlichkeitSeite.Unterdialog.Keins;
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) =>
        {
            gefragt = a;
            return LeererSatz();
        }));

        Einstieg(cut, erwartet).Click();

        Assert.Equal(erwartet, gefragt);
        Assert.Equal(erwartet, cut.Instance.OffenerUnterdialog);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    // =====================================================================
    // Die zwei Tarif-Sprünge (E3/7)
    // =====================================================================

    /// <summary>
    /// <b>E3/7 — der Sprung wird zur ÜBERLAGERUNG.</b> Bis dahin öffnete der
    /// Knopf „BHKW-Tarif…" ein ZWEITES WinForms-Fenster über dem ersten (Risiko
    /// R2); in der Überlagerung meldete der Dialog seinen Sprung, und dieser
    /// Wirt verwarf ihn — der Knopf tat dort gar nichts. Jetzt schließt der
    /// Dialog (er hat im OK-Weg geschrieben), und die Tarifstruktur geht in der
    /// gewünschten SICHT im selben Fenster auf.
    ///
    /// <para><b>Q11 (E7b):</b> Der zweite Sprung „Strombezug…" ist entfallen. Die
    /// Tarif-Überlagerung ist seither nur noch über die Sprünge erreichbar — deshalb
    /// prüft dieser Fall auch „ein Titel, ein Kreuz", den bis dahin der Knopf
    /// „Strombezug…" in den zwei Fällen unten trug.</para>
    /// </summary>
    [Fact]
    public void Der_Bhkw_Sprung_oeffnet_die_Tarifstruktur_in_der_Sicht_Bhkw()
    {
        var gefragt = new List<WirtschaftlichkeitSeite.Unterdialog>();
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) =>
        {
            gefragt.Add(a);
            return LeererSatz();
        }));

        Einstieg(cut, WirtschaftlichkeitSeite.Unterdialog.Bhkw).Click();

        var dialog = cut.FindComponent<BhkwWirtschaftlichkeitDialog>();
        cut.InvokeAsync(() => dialog.Instance.Geschlossen.InvokeAsync(
            new BhkwWirtschaftlichkeitErgebnis(true, BhkwSprung.BhkwTarif)));

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.TarifBhkw, cut.Instance.OffenerUnterdialog);
        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.TarifBhkw, gefragt[^1]);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
        Assert.Single(cut.FindComponents<TarifstrukturDialog>());

        // Ein Titel, eine Stelle: die Überlagerung trägt Titel UND Kreuz, der Dialog keins.
        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung h1.epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-ueberlagerung-titel"));
    }

    /// <summary>
    /// Ohne Sprung schließt der BHKW-Dialog wie bisher — die Gegenprobe zum
    /// Fall darüber.
    /// </summary>
    [Fact]
    public void Ohne_Sprung_schliesst_der_Bhkw_Dialog_nur()
    {
        var cut = Zeige(p => p.Add(x => x.Gaben,
            (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Einstieg(cut, WirtschaftlichkeitSeite.Unterdialog.Bhkw).Click();

        var dialog = cut.FindComponent<BhkwWirtschaftlichkeitDialog>();
        cut.InvokeAsync(() => dialog.Instance.Geschlossen.InvokeAsync(
            new BhkwWirtschaftlichkeitErgebnis(true, BhkwSprung.Keiner)));

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
    }

    /// <summary>
    /// Dasselbe für den PV-Vergütungsdialog: Sein Sprungknopf nimmt seit #405
    /// den OK-Weg (prüfen, schreiben, springen) — das Ziel ist die Tarifstruktur
    /// in der Sicht Photovoltaik (E3/7).
    /// </summary>
    [Fact]
    public void Der_Pv_Sprung_oeffnet_die_Tarifstruktur_in_der_Sicht_Photovoltaik()
    {
        var gefragt = new List<WirtschaftlichkeitSeite.Unterdialog>();
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) =>
        {
            gefragt.Add(a);
            return LeererSatz();
        }));

        Einstieg(cut, WirtschaftlichkeitSeite.Unterdialog.Photovoltaik).Click();

        var dialog = cut.FindComponent<PhotovoltaikVerguetungDialog>();
        cut.InvokeAsync(() => dialog.Instance.Geschlossen.InvokeAsync(
            new PvVerguetungErgebnis(true, PvSprung.Tarif)));

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.TarifPv, cut.Instance.OffenerUnterdialog);
        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.TarifPv, gefragt[^1]);
        Assert.Single(cut.FindComponents<TarifstrukturDialog>());
    }

    /// <summary>
    /// Ein Titel, eine Stelle (Befund „Doppeltes Kreuz dürfen nicht sein!",
    /// 15.09.2026): Jede Überlagerung trägt Titel UND Kreuz, der eingebettete Dialog
    /// darin keins von beiden — über <c>TitelAnzeigen="false"</c>, RECHTS vom
    /// Parametersatz der Hülle und deshalb auch dann gültig, wenn dieser einen Titel
    /// mitbrächte. ETAPPE E6: Der Kapitalwert-Verlauf ist keine Überlagerung mehr.
    /// </summary>
    [Theory]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    public void Jede_Ueberlagerung_zeigt_nur_ein_Kreuz_und_einen_Titel(
        WirtschaftlichkeitSeite.Unterdialog art)
    {
        var cut = Zeige(p => p.Add(x => x.Gaben,
            (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Einstieg(cut, art).Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    [Fact]
    public void Ohne_Parametersatz_bleibt_der_Bereich_zu_und_die_Huelle_meldet()
    {
        var cut = Zeige(p => p
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _)
                => (IReadOnlyDictionary<string, object>?)null)
            .Add(x => x.Nachlauf, (WirtschaftlichkeitSeite.Unterdialog _, bool _)
                => "Zu diesem Projekt gibt es keine BHKW-Vergleichsgruppe."));

        Fussknoepfe(cut)[1].Click();   // BHKW

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Empty(cut.FindAll(".epos-ueberlagerung"));
        Assert.Contains("keine BHKW-Vergleichsgruppe", cut.Instance.Status);
    }

    [Fact]
    public void Nach_dem_Schliessen_frischt_die_Seite_auf_und_meldet()
    {
        var cut = Zeige(p => p
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz())
            .Add(x => x.Nachlauf, (WirtschaftlichkeitSeite.Unterdialog a, bool ok)
                => ok ? "Parameter gespeichert — bitte neu berechnen." : ""));

        Einstieg(cut, WirtschaftlichkeitSeite.Unterdialog.Parameter).Click();
        // Esc auf der Ueberlagerung schliesst ohne zu speichern.
        cut.Find(".epos-ueberlagerung").KeyDown("Escape");

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Equal(2, _geladen);                            // Aufbau + Nachlauf
    }

    /// <summary>
    /// Auftrag #286, Einbettungsstelle 2: Der BHKW-Dialog schreibt auch hier nur im
    /// OK-Weg. Nach OK bekommt der Wirt seine Speichermeldung und rechnet neu, nach
    /// Abbrechen bekommt er nichts — und die Datenbank sieht keinen Zugriff.
    ///
    /// <para><b>ETAPPE E2 (Befund 04/B30).</b> Der OK-Weg schreibt nur noch den
    /// WERTLICH geaenderten Stand. Geaendert ist hier allein die Anlagenzeile — die
    /// Projektvorgaben bleiben, wie sie geladen wurden, und werden deshalb nicht
    /// mitgeschrieben. Aus den zwei Zugriffen wird EINER; die Aussage des Falls
    /// bleibt dieselbe: geschrieben wird im OK-Weg und sonst nirgends.</para>
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Der_BHKW_Dialog_schreibt_in_der_Ueberlagerung_nur_im_OK_Weg(bool ok)
    {
        var anlage = new KwkgAnlagenAngabe
        { IdAnlage = 14920, IdProjekt = 1030, Bezeichner = "BHKW 50", PelKW = 50 };
        var parameter = new WirtschaftlichkeitParameter();
        int zugriffe = 0;
        bool? gemeldet = null;

        var gaben = new Dictionary<string, object>
        {
            ["IdStamm"] = 1030,
            ["Anlagen"] = (IList<KwkgAnlagenAngabe>)new List<KwkgAnlagenAngabe> { anlage },
            ["Parameter"] = parameter,
            ["SpeichereAnlage"] = new Func<KwkgAnlagenAngabe, bool>(_ => { zugriffe++; return true; }),
            ["SpeichereVorgaben"] = new Func<WirtschaftlichkeitParameter, bool>(
                _ => { zugriffe++; return true; })
        };

        var cut = Zeige(p => p
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _)
                => (IReadOnlyDictionary<string, object>)gaben)
            .Add(x => x.Nachlauf, (WirtschaftlichkeitSeite.Unterdialog _, bool g)
                => { gemeldet = g; return g ? "BHKW-Wirtschaftlichkeit gespeichert." : ""; }));

        Fussknoepfe(cut)[1].Click();                  // BHKW
        cut.FindAll("input[inputmode=decimal]")[0].Input("5,57");
        Assert.Equal(0, zugriffe);

        // Die Leiste des Dialogs traegt Abbrechen (0) und Speichern (1).
        IReadOnlyList<IElement> knoepfe = cut.FindAll(".epos-ueberlagerung .epos-leiste button");
        knoepfe[ok ? 1 : 0].Click();

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Equal(ok ? 1 : 0, zugriffe);          // nur die geaenderte Anlagenzeile
        Assert.Equal(ok ? 5.57 : (double?)null, anlage.SatzEinspCt);
        Assert.Equal(ok, gemeldet);

        // Nach OK setzt der Nachlauf die Meldung; nach Abbrechen sagt er nichts, und
        // die Seite behaelt ihre eigene Statuszeile.
        if (ok) Assert.Equal("BHKW-Wirtschaftlichkeit gespeichert.", cut.Instance.Status);
        else Assert.DoesNotContain("BHKW-Wirtschaftlichkeit gespeichert.", cut.Instance.Status);
    }

    /// <summary>
    /// Ein Titel, eine Stelle (W11b‑B‑9): Die Überlagerung trägt ihn, der Dialog
    /// darin zeigt keinen eigenen Kopf — und zwar JEDER. Die
    /// Geschwister des BHKW-Dialogs beziehen ihren Titel aus einem eigenen
    /// Ausdruck; deshalb sah die Markup-Wache sie bis #289 nicht, und deshalb
    /// steht hier der GEZEICHNETE Nachweis: genau ein Titel je Bereich, der der
    /// Überlagerung.
    /// </summary>
    [Theory]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    public void Kein_Unterdialog_zeigt_in_der_Ueberlagerung_einen_eigenen_Titel(
        WirtschaftlichkeitSeite.Unterdialog art)
    {
        var cut = Zeige(p => p.Add(x => x.Gaben,
            (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Einstieg(cut, art).Click();

        Assert.Empty(cut.FindAll(".epos-ueberlagerung h1.epos-dialog-titel"));
        Assert.Single(cut.FindAll(".epos-ueberlagerung .epos-ueberlagerung-titel"));

        // Der Hilfeknopf des Dialogs bleibt — nur der Kopf faellt weg.
        Assert.NotEmpty(cut.FindAll(".epos-ueberlagerung .epos-dialog-kopf--ohnetitel"));
    }

    // =====================================================================
    // Berechnen
    // =====================================================================

    [Fact]
    public void Berechnen_uebergibt_die_Varianten_ohne_Stamm()
    {
        IReadOnlyList<int>? ids = null;
        var cut = Zeige(p => p.Add(x => x.Berechnen,
            (IReadOnlyList<int> v, Action<Laufschritt> m) =>
            {
                ids = v;
                return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "Berechnet." });
            }));

        Fussknoepfe(cut)[0].Click();   // „Berechnen" (ohne Gaben der einzige Knopf)

        Assert.NotNull(ids);
        Assert.Equal(new[] { 1031 }, ids!);
        Assert.Equal("Berechnet.", cut.Instance.Status);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Der_Fortschritt_zaehlt_und_verschwindet_danach()
    {
        var cut = Zeige(p => p.Add(x => x.Berechnen,
            (IReadOnlyList<int> v, Action<Laufschritt> m) =>
            {
                m(new Laufschritt(1, 2, "Stammprojekt"));
                return Task.FromResult(new LaufErgebnis { Erfolg = true, Statuszeile = "Berechnet." });
            }));

        Fussknoepfe(cut)[0].Click();

        Assert.Empty(cut.FindAll(".epos-fortschritt"));
        Assert.False(cut.Instance.Beschaeftigt);
    }

    [Fact]
    public void Ein_Abbruch_meldet_sich_und_laedt_nicht_neu()
    {
        var cut = Zeige(p => p
            .Add(x => x.StatusAbgebrochen, "Vorgang abgebrochen.")
            .Add(x => x.Berechnen, (IReadOnlyList<int> v, Action<Laufschritt> m)
                => Task.FromResult(new LaufErgebnis { Abgebrochen = true })));

        Fussknoepfe(cut)[0].Click();

        Assert.Equal("Vorgang abgebrochen.", cut.Instance.Status);
        Assert.Equal(1, _geladen);
    }

    [Fact]
    public void Ein_Rechenfehler_erscheint_als_Warnbanner()
    {
        var cut = Zeige(p => p.Add(x => x.Berechnen, (IReadOnlyList<int> v, Action<Laufschritt> m)
            => Task.FromResult(new LaufErgebnis { Fehler = "Simulation gescheitert." })));

        Fussknoepfe(cut)[0].Click();

        Assert.Contains("Simulation gescheitert", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var cut = Zeige();

        Assert.Equal("UcWirtschaftlichkeit.btn_Help", cut.Instance.HilfeSchluessel);
    }

    // =====================================================================
    //  Die Vergleichswahl (Anwenderwunsch 08.09.2026, W5-B-5)
    // =====================================================================

    [Fact]
    public void Der_Haken_einer_Variante_meldet_die_Vergleichswahl_und_zeigt_die_Tabelle_neu()
    {
        IReadOnlyList<int>? gemeldet = null;
        int angezeigt = 0;
        var cut = Zeige(p => p
            .Add(x => x.VergleichGewaehlt, (IReadOnlyList<int> l) => gemeldet = l)
            .Add(x => x.Anzeigen, (int id) => { angezeigt++; return Ansicht("1 €"); }));

        cut.FindAll(".epos-raster tbody input[type=checkbox]")[1].Change(false);

        Assert.Equal(new[] { 1030 }, gemeldet);
        Assert.Equal(1, angezeigt);
        Assert.Equal(new[] { 1030 }, cut.Instance.Gewaehlte);
        Assert.Contains("1 €", cut.Markup);
    }

    // =====================================================================
    //  Das Warnband und der Rechenweg (Auftrag #267, Anwenderbefund 14.09.2026)
    // =====================================================================

    /// <summary>Die Bänder des Warnbandes über den Kennzahlkarten.</summary>
    private static IReadOnlyList<IElement> Warnbaender(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-wirt-warnband .epos-warnbanner");

    /// <summary>Eine Ansicht mit einer Hinweiszeile, wie die Hülle sie baut: der
    /// Fehlgrund des Kerns, mit dem Warnzeichen davor, in jeder Spalte.</summary>
    private static ErgebnisAnsicht MitWarnung(string text, string zweite = "")
    {
        ErgebnisAnsicht a = Ansicht();
        var zeilen = new List<MatrixZeile>(a.Matrix.Zeilen)
        {
            new MatrixZeile
            {
                Titel = "Hinweis",
                Zellen = new[] { ErgebnisMatrix.WARN_PRAEFIX + text, ErgebnisMatrix.WARN_PRAEFIX + text }
            }
        };
        if (zweite.Length > 0)
            zeilen.Add(new MatrixZeile
            {
                Titel = "Hinweis",
                Zellen = new[] { ErgebnisMatrix.WARN_PRAEFIX + zweite, "" }
            });
        a.Matrix = new ErgebnisMatrix { Spalten = a.Matrix.Spalten, Zeilen = zeilen };
        return a;
    }

    /// <summary>
    /// <b>DER BEFUND.</b> „Die Energiekosten sind 0 auch nach Berechnung. Kosten sind
    /// angegeben." Der Grund stand als letzte Zeile „Hinweis" unter zwanzig
    /// Kennzahlzeilen — gelesen wurde er nicht. Jetzt steht er als Warnband GANZ OBEN,
    /// vor den Karten, deren „—" er erklärt.
    /// </summary>
    [Fact]
    public void Ein_Fehlgrund_der_Tabelle_steht_als_Warnband_ueber_den_Karten()
    {
        const string grund = "Energiekosten nicht bestimmbar: Der elektrischen Erzeugung " +
                             "ist kein Energieträger zugeordnet.";
        WirtschaftlichkeitStand stand = Standard();
        stand.Ansicht = MitWarnung(grund);
        var cut = Zeige(stand: stand);

        IReadOnlyList<IElement> baender = Warnbaender(cut);
        Assert.Single(baender);                       // je Meldung EINES, nicht je Spalte
        Assert.Contains(grund, baender[0].TextContent);

        // Vor den Karten — sonst liest der Anwender wieder zuerst das „—".
        Assert.True(cut.Markup.IndexOf("epos-wirt-warnband", StringComparison.Ordinal) <
                    cut.Markup.IndexOf("epos-kennzahlkachel", StringComparison.Ordinal));
    }

    /// <summary>Mehrere Hinweise des Kerns kommen mit „ | " aneinandergehängt an —
    /// im Band wird daraus je eine eigene Zeile.</summary>
    [Fact]
    public void Mehrere_Hinweise_werden_zu_je_einem_Band()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Ansicht = MitWarnung("Erster Grund. | Zweiter Grund.");
        var cut = Zeige(stand: stand);

        Assert.Equal(2, Warnbaender(cut).Count);
        Assert.Equal(new[] { "Erster Grund.", "Zweiter Grund." }, cut.Instance.Warnungen);
    }

    /// <summary>
    /// <b>Der Weg steht neben dem Grund.</b> Zweiter Befund derselben Abnahme: Die
    /// Fußzeile verlangte „bitte neu berechnen", der Knopf „Berechnen" lag unter der
    /// langen Tabelle außer Sicht. Der Knopf im Band startet denselben Lauf.
    /// </summary>
    [Fact]
    public async Task Das_Warnband_traegt_den_Rechenknopf()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Ansicht = MitWarnung("Energiekosten nicht bestimmbar.");
        int laeufe = 0;
        var cut = Zeige(p => p.Add(x => x.Berechnen, (IReadOnlyList<int> v, Action<Laufschritt> m)
            => { laeufe++; return Task.FromResult(new LaufErgebnis()); }), stand);

        IElement knopf = cut.Find(".epos-wirt-warnband .epos-leiste button");
        Assert.Equal("Neu berechnen", knopf.TextContent.Trim());
        await cut.InvokeAsync(() => knopf.Click());

        Assert.Equal(1, laeufe);
    }

    /// <summary>
    /// Ohne Warnung kein Band — und ohne Band kein zweiter Rechenknopf. Die Seite
    /// bleibt ruhig, solange nichts zu sagen ist.
    /// </summary>
    [Fact]
    public void Ohne_Warnung_gibt_es_kein_Band()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-wirt-warnband"));
        Assert.Empty(cut.Instance.Warnungen);
    }

    /// <summary>
    /// „Parameter gespeichert — bitte neu berechnen." war eine Aufforderung ohne Weg.
    /// Nach dem Schließen eines Unterdialogs steht das Band mit dem Rechenknopf da,
    /// auch wenn die Tabelle keine Warnung führt.
    /// </summary>
    [Fact]
    public async Task Nach_dem_Speichern_eines_Unterdialogs_steht_der_Rechenknopf_oben()
    {
        var cut = Zeige(p => p
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) => LeererSatz())
            .Add(x => x.Nachlauf, (WirtschaftlichkeitSeite.Unterdialog a, bool ok)
                => ok ? "Parameter gespeichert — bitte neu berechnen." : "")
            .Add(x => x.Berechnen, (IReadOnlyList<int> v, Action<Laufschritt> m)
                => Task.FromResult(new LaufErgebnis())));

        Assert.Empty(cut.FindAll(".epos-wirt-warnband"));

        Einstieg(cut, WirtschaftlichkeitSeite.Unterdialog.Parameter).Click();
        var dialog = cut.FindComponent<WirtschaftlichkeitParameterDialog>();
        await cut.InvokeAsync(() => dialog.Instance.Geschlossen.InvokeAsync(
            new WirtParameterErgebnis(true)));

        Assert.Single(cut.FindAll(".epos-wirt-warnband .epos-leiste button"));
        Assert.Contains("bitte neu berechnen", cut.Find(".epos-wirt-warnband .epos-warnbanner").TextContent);
    }

    /// <summary>
    /// ETAPPE E8a (U48): Die <b>Fußzeile</b> von „Was ist angenommen?" nennt die gerechneten
    /// Szenarien und die Herkunft der Annahmen — „Vorgaben" ohne Pflege, „gepflegt" nach
    /// einer Pflege im Parameterdialog: Dessen OK frischt die Seite auf, und die Hülle bildet
    /// die Zeile aus dem neu gelesenen Parametersatz.
    /// </summary>
    [Fact]
    public async Task Die_Fusszeile_nennt_Vorgaben_und_nach_einer_Pflege_gepflegt()
    {
        const string vorgaben = "Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt";
        const string gepflegt = "Drei Szenarien gerechnet · Annahmen gepflegt: Kalkulationszins";
        bool gespeichert = false;
        var cut = Render<WirtschaftlichkeitSeite>(p => p
            .Add(x => x.Laden, () =>
            {
                WirtschaftlichkeitStand s = Standard();
                s.Ansicht.Szenariofuss = gespeichert ? gepflegt : vorgaben;
                return s;
            })
            .Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) => LeererSatz())
            .Add(x => x.Nachlauf, (WirtschaftlichkeitSeite.Unterdialog a, bool ok)
                => ok ? "Parameter gespeichert — bitte neu berechnen." : ""));

        IElement fuss = cut.Find(".epos-wirt-szenariofuss");
        Assert.Equal(vorgaben, fuss.TextContent);
        // Sie steht im Fuß von „Was ist angenommen?" — E8a‑Q4: links in DERSELBEN Reihe
        // wie die Knöpfe „Anhang-E-Checkliste…" und „Bericht erzeugen".
        IElement annahmen = cut.FindAll("section.epos-gruppenkopf")[3];
        Assert.Contains("Was ist angenommen?", annahmen.TextContent);
        Assert.NotNull(annahmen.QuerySelector(".epos-wirt-szenariofuss"));
        Assert.Contains("epos-wirt-abschnitt-fuss", fuss.ParentElement!.ClassList);
        Assert.Contains("epos-wirt-checklistenknopf", fuss.NextElementSibling!.ClassList);

        Einstieg(cut, WirtschaftlichkeitSeite.Unterdialog.Parameter).Click();
        var dialog = cut.FindComponent<WirtschaftlichkeitParameterDialog>();
        gespeichert = true;                                       // die Pflege im Dialog
        await cut.InvokeAsync(() => dialog.Instance.Geschlossen.InvokeAsync(new WirtParameterErgebnis(true)));

        Assert.Equal(gepflegt, cut.Find(".epos-wirt-szenariofuss").TextContent);
        Assert.Contains("gepflegt", cut.Find(".epos-wirt-szenariofuss").TextContent);
    }

    /// <summary>
    /// Eine veraltete Statuszeile (Warnzeichen der Hülle) stellt dasselbe Band auf —
    /// die angezeigten Zahlen stammen dann aus einem älteren Lauf.
    /// </summary>
    [Fact]
    public void Eine_veraltete_Statuszeile_stellt_den_Rechenknopf_auf()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Statuszeile = "⚠ Gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand.";
        var cut = Zeige(p => p.Add(x => x.Berechnen, (IReadOnlyList<int> v, Action<Laufschritt> m)
            => Task.FromResult(new LaufErgebnis())), stand);

        Assert.Single(cut.FindAll(".epos-wirt-warnband .epos-leiste button"));
    }

    /// <summary>
    /// <b>Anwenderbefund 22.09.2026.</b> Ist das SIMULATIONSergebnis einer gewählten
    /// Version älter als die letzte Änderung ihres Projekts, sagt das Band es. Bis
    /// hierher stand das allein als Farbe in der Spalte „Simulation“ — der Anwender
    /// rechnete die Wirtschaftlichkeit neu und bekam dieselben Zahlen, weil der LAUF
    /// alt war und nicht die Rechnung darüber.
    /// </summary>
    [Fact]
    public void Ein_veraltetes_Simulationsergebnis_stellt_das_Band_auf()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Varianten[1].Veraltet = true;

        var cut = Zeige(null, stand);

        Assert.True(cut.Instance.SimulationVeraltet);
        Assert.Contains("Simulationsergebnis",
                        cut.Find(".epos-wirt-warnband").TextContent);
    }

    /// <summary>
    /// DIE GEGENPROBE: Dieselbe veraltete Version, aber ABGEWÄHLT. Sie steht in keiner
    /// Zahl dieser Seite — ihr alter Lauf ist hier also kein Anlass, und das Band
    /// bleibt weg.
    /// </summary>
    [Fact]
    public void Eine_abgewaehlte_Version_stellt_kein_Band_auf()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Varianten[1].Veraltet = true;
        stand.GewaehlteVarianten = new[] { 1030 };

        var cut = Zeige(null, stand);

        Assert.False(cut.Instance.SimulationVeraltet);
        Assert.Empty(cut.FindAll(".epos-wirt-warnband"));
    }

    /// <summary>
    /// Der Hinweis „Strombedarf ohne Verwendung“ des Kerns (Anwenderentscheid
    /// 22.09.2026) reist als Hinweiszeile der Vergleichstabelle und wird oben im Band
    /// gezeichnet — dort, wo der Anwender die Erklärung zu den Zahlen sucht.
    /// </summary>
    [Fact]
    public void Der_Hinweis_Strombedarf_ohne_Verwendung_steht_im_Band()
    {
        const string satz = "Strombedarf ohne Verwendung: Das Projekt führt einen " +
                            "Strombedarf von 16,1 MWh/a, aber keinen Erzeuger, der Strom " +
                            "verwendet. Die Energiekosten sind ohne Stromkosten bestimmt.";

        WirtschaftlichkeitStand stand = Standard();
        stand.Ansicht.Matrix = new ErgebnisMatrix
        {
            Spalten = new[] { "Kennzahl", "Stamm" },
            Zeilen = new[]
            {
                new MatrixZeile { Titel = "Hinweis",
                                  Zellen = new[] { ErgebnisMatrix.WARN_PRAEFIX + satz } }
            }
        };

        var cut = Zeige(null, stand);

        Assert.Contains(satz, cut.Instance.Warnungen);
        Assert.Contains("Strombedarf ohne Verwendung",
                        cut.Find(".epos-wirt-warnband").TextContent);
    }
    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F4)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Seite an der Maskenbrücke.</b> Jede Wahl läuft durch
    /// denselben Rückruf wie ein Griff in die Klappliste: Die Seite holt sich dabei
    /// einen NEUEN Stand aus der Hülle.
    /// </summary>
    [Fact]
    public void Die_Seite_meldet_sich_beim_Assistenten_an_und_waehlt_ein_Szenario()
    {
        var cut = Zeige();

        Assert.True(KiMaskenbruecke.IstAngemeldet(
            KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE));

        KiFeldzugang szenario = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, "szenario");
        Assert.NotNull(szenario);
        Assert.True(szenario.Setzbar);
        Assert.NotEmpty(szenario.Wahleintraege());

        string zweites = szenario.Wahleintraege()[1].Text;
        KiFeldumsetzung wahl = KiFeldwandler.Wandle(szenario, zweites);
        Assert.True(wahl.Ok, wahl.Grund);
        szenario.Setzen(wahl.Wert);
        cut.Render();

        Assert.Equal(szenario.Wahleintraege()[1].Schluessel,
                     Convert.ToString(szenario.Lesen(), CultureInfo.InvariantCulture));

        // Die nicht monetarisierbaren Wirkungen sind Felder dieser Seite (ETAPPE E17: die
        // Zahl der Zeilen und je Zeile die Spalten wirkung_*).
        KiFeldzugang wirkung = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, "wirkung_anzahl");
        Assert.NotNull(wirkung);
        Assert.True(wirkung.Setzbar);

        // Die Vergleichsgruppe ist eine Menge von Verweisen und KEIN Feld.
        Assert.Null(KiDialoge.Katalog.Finde(KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE)!
                              .FindeFeld("gewaehlte_varianten"));
    }

    /// <summary>
    /// ETAPPE E8a, KI‑D‑Q11: Die zwei Klapplisten von Block 2 („Zahlungsreihen") sind
    /// ANZEIGEWAHLEN und stehen in der Feldkarte. Gelesen wird die GEZEIGTE Tafel
    /// (Vorgabe: die Leitversion im Erwartungsfall, samt Rückfall); gesetzt wird durch
    /// dieselben Rückrufe wie die Klapplisten — ohne neuen Stand aus der Hülle.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_Stand_und_Szenario_der_Zahlungsreihen()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Darstellung = WirtschaftlichkeitStand.DARSTELLUNG_VALERI;
        stand.Ansicht.Leitversion = 1031;
        stand.Ansicht.Zahlungsstaende = new[] { (1030, "Stamm"), (1031, "WP klein") };
        stand.Ansicht.Zahlungsreihen = new[]
        {
            Zahlungstafel(1030, 0, "S0"), Zahlungstafel(1031, 0, "W0"), Zahlungstafel(1031, 2, "W2")
        };
        var cut = Zeige(stand: stand);
        int geladen = _geladen;

        KiFeldzugang standFeld = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, "zahlungsreihen_stand");
        KiFeldzugang szenarioFeld = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, "zahlungsreihen_szenario");
        Assert.NotNull(standFeld);
        Assert.NotNull(szenarioFeld);
        Assert.True(standFeld.Setzbar);
        Assert.True(szenarioFeld.Setzbar);
        Assert.Equal(new[] { "Stamm", "WP klein" },
                     standFeld.Wahleintraege().Select(e => e.Text).ToArray());

        // Gelesen wird, was die Seite zeigt: die Leitversion im Erwartungsfall.
        Assert.Equal("1031", Convert.ToString(standFeld.Lesen(), CultureInfo.InvariantCulture));
        Assert.Equal("0", Convert.ToString(szenarioFeld.Lesen(), CultureInfo.InvariantCulture));

        KiFeldumsetzung wahl = KiFeldwandler.Wandle(szenarioFeld, "Worst Case");
        Assert.True(wahl.Ok, wahl.Grund);
        szenarioFeld.Setzen(wahl.Wert);
        cut.Render();
        Assert.Equal("W2", cut.Find(".epos-wirt-zahlungsreihen tbody tr td").TextContent.Trim());
        Assert.Equal("2", Convert.ToString(szenarioFeld.Lesen(), CultureInfo.InvariantCulture));

        // Der Stamm trägt kein „Worst Case": Die Seite fällt auf den Erwartungsfall
        // zurück, und so liest ihn auch der Assistent.
        wahl = KiFeldwandler.Wandle(standFeld, "Stamm");
        Assert.True(wahl.Ok, wahl.Grund);
        standFeld.Setzen(wahl.Wert);
        cut.Render();
        Assert.Equal("S0", cut.Find(".epos-wirt-zahlungsreihen tbody tr td").TextContent.Trim());
        Assert.Equal(1030, cut.Instance.GezeigteZahlungsreihe!.IdStand);
        Assert.Equal("0", Convert.ToString(szenarioFeld.Lesen(), CultureInfo.InvariantCulture));

        // Eine Anzeigewahl: Die Seite holt dafür keinen neuen Stand und lässt das
        // Szenario der Kennzahlen stehen.
        Assert.Equal(geladen, _geladen);
        Assert.Equal(0, _stand.SzenarioId);
    }

    /// <summary>Eine Probetafel der Zahlungsreihen: eine Jahreszeile, deren erste Zelle
    /// <paramref name="kennung"/> trägt.</summary>
    private static ZahlungsreihenTafel Zahlungstafel(int stand, int szenario, string kennung) => new ZahlungsreihenTafel
    {
        IdStand = stand,
        Szenario = szenario,
        Tafel = new ErgebnisMatrix
        {
            Spalten = new[] { "Jahr", "Netto nominal" },
            Zeilen = new[] { new MatrixZeile { Titel = "0", Zellen = new[] { kennung } } }
        }
    };
}
