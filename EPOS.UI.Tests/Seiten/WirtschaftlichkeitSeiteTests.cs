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
/// DIN EN 17463 darunter (AUFTRAG #325), die Sicht-Knöpfe (Photovoltaik, BHKW,
/// Strombezug — je nach Ausstattung), „Verlauf…", „Berechnen" und der
/// Abbrechen-Knopf während eines Laufs.</para>
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

    private static WirtschaftlichkeitStand Standard(bool pv = true, bool bhkw = true,
                                                    bool strom = true) => new WirtschaftlichkeitStand
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
        MitStrombezug = strom,
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
            : Fussknoepfe(cut)[art switch
            {
                WirtschaftlichkeitSeite.Unterdialog.Photovoltaik => 0,
                WirtschaftlichkeitSeite.Unterdialog.Bhkw => 1,
                WirtschaftlichkeitSeite.Unterdialog.Strombezug => 2,
                _ => 3                                   // Verlauf
            }];

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
        Assert.Contains("Referenz: Stammprojekt", cut.Find(".epos-herleitung-text").TextContent);
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
    /// ETAPPE W5‑B‑11 (VALERI-Lücke G9): Der Vorschlag zur Entscheidung steht
    /// unmittelbar UNTER der Vergleichstabelle — dort, wo die Zahlen stehen, aus
    /// denen er sich ergibt.
    /// </summary>
    [Fact]
    public void Die_Empfehlungszeile_steht_unter_der_Vergleichstabelle()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.Ansicht.Empfehlungszeile =
            "Vorschlag zur Entscheidung: Variante „WP klein“ — " +
            "Kapitalwertdifferenz zum Stammprojekt +12.300 € (Erwartet).";
        var cut = Zeige(stand: stand);

        var texte = cut.FindAll(".epos-herleitung-text").Select(e => e.TextContent).ToList();
        Assert.Contains(texte, z => z.StartsWith("Vorschlag zur Entscheidung:"));

        // Reihenfolge im gezeichneten Baum: erst die Matrix, dann die Zeile.
        var knoten = cut.FindAll(".epos-matrix, .epos-herleitung-text");
        int matrix = knoten.ToList().FindIndex(e => e.ClassList.Contains("epos-matrix"));
        int satz = knoten.ToList().FindIndex(
            e => e.TextContent.StartsWith("Vorschlag zur Entscheidung:"));
        Assert.True(matrix >= 0 && satz > matrix);
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
    /// PV, BHKW, Strombezug, Verlauf und Berechnen. Der Einstieg selbst ist nicht
    /// verschwunden, er steht in der Zeile der Szenariowahl.
    /// </summary>
    [Fact]
    public void Die_drei_Sichtknoepfe_folgen_der_Ausstattung()
    {
        var alle = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));
        Assert.Equal(5, Fussknoepfe(alle).Count);   // PV, BHKW, Strom, Verlauf, Berechnen

        var ohne = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()),
                         stand: Standard(pv: false, bhkw: false, strom: false));
        Assert.Equal(2, Fussknoepfe(ohne).Count);   // Verlauf, Berechnen
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

        // Die Zeile trägt das Auswahlfeld UND den Knopf.
        IElement zeile = cut.Find(".epos-seite-zeile");
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
    /// Der Einstieg „Strombezug…" hängt allein am TARIFSATZ des Projekts
    /// (<c>MitStrombezug</c>), nicht an der Erzeugerlage der Gruppe: Er pflegt
    /// die Sicht „Strombezug" des Tarifsatzes, und die wirkt nur, solange der
    /// Satz aktiv ist. Ein Wärmepumpenprojekt ohne aktiven Tarif zeigt ihn
    /// deshalb nicht — es gibt dort nichts zu pflegen, was rechnet.
    /// </summary>
    [Fact]
    public void Der_Strombezug_Einstieg_haengt_allein_am_Tarifsatz()
    {
        // Aktiver Tarifsatz — der Knopf steht da, auch ohne PV und ohne BHKW.
        var mitTarif = Zeige(
            p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()),
            stand: Standard(pv: false, bhkw: false, strom: true));
        Assert.Contains(Fussknoepfe(mitTarif), k => k.TextContent.Trim() == "Strombezug…");

        // Kein aktiver Tarifsatz — der Knopf fehlt, gleich welcher Erzeuger in
        // der Vergleichsgruppe steht (die Wärmepumpe ankert ihn nicht mehr).
        var ohneTarif = Zeige(
            p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()),
            stand: Standard(pv: false, bhkw: false, strom: false));
        Assert.DoesNotContain(Fussknoepfe(ohneTarif), k => k.TextContent.Trim() == "Strombezug…");
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

    [Fact]
    public void Ein_Szenariowechsel_zeigt_neu_ohne_zu_rechnen()
    {
        int gefragt = -1;
        var cut = Zeige(p => p.Add(x => x.Anzeigen, (int id) =>
        {
            gefragt = id;
            return Ansicht("9.000 €");
        }));

        cut.Find("select").Change("2");

        Assert.Equal(2, gefragt);
        Assert.Equal("9.000 €", cut.FindAll(".epos-kennzahlkachel-wert")[0].TextContent);
        Assert.Equal(1, _geladen);   // NICHT neu geladen
    }

    // =====================================================================
    // AUFTRAG #325 — Bewertung nach DIN EN 17463 auf der Seite
    // =====================================================================

    /// <summary>
    /// <b>Der Block steht unter der Kennzahltabelle und ist zugeklappt.</b>
    /// Anwenderentscheid 17.09.2026: „heraus nehmen aus Parameter Dialog: 1.
    /// Bewertung nach DIN EN 17463" — in die Wirtschaftlichkeitsseite, weil der Text
    /// dort steht, wo auch der Kapitalwert steht.
    ///
    /// <para>Kein Delegat, kein Knopf: Ohne Schreibweg gibt es den Block nicht.</para>
    /// </summary>
    [Fact]
    public void Der_Bewertungsblock_steht_unter_der_Tabelle_und_haengt_am_Schreibweg()
    {
        var ohne = Zeige();
        Assert.Empty(ohne.FindAll("button.epos-modulparameter-knopf"));

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (string _) => true));

        IElement knopf = cut.Find("button.epos-modulparameter-knopf");
        Assert.Equal("false", knopf.GetAttribute("aria-expanded"));
        Assert.Contains("Bewertung nach DIN EN 17463", knopf.TextContent);

        // Zugeklappt steht das Feld nicht da.
        Assert.Empty(cut.FindAll("textarea"));
        Assert.False(cut.Instance.BewertungOffen);

        knopf.Click();

        Assert.Equal("true", cut.Find("button.epos-modulparameter-knopf").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll("textarea"));
        Assert.True(cut.Instance.BewertungOffen);
    }

    /// <summary>
    /// <b>Der Block zeigt den gepflegten Text und schreibt ihn auf Zuruf fort.</b>
    /// Geschrieben wird über denselben Weg, den bis zu diesem Auftrag der
    /// Parameterdialog nahm (<c>WirtschaftlichkeitCtrl.SpeichereParameter</c> in der
    /// Hülle) — und erst auf Knopfdruck, nicht bei jedem Tastendruck.
    /// </summary>
    [Fact]
    public void Der_Bewertungsblock_zeigt_den_gepflegten_Text_und_schreibt_ihn_fort()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.NichtMonetaer = "Versorgungssicherheit";

        string? geschrieben = null;
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (string t) =>
        {
            geschrieben = t;
            return true;
        }), stand: stand);

        cut.Find("button.epos-modulparameter-knopf").Click();
        Assert.Equal("Versorgungssicherheit", cut.Find("textarea").TextContent);

        cut.Find("textarea").Input("Versorgungssicherheit, Arbeitsschutz");

        // Bis zum Knopfdruck ist nichts geschrieben.
        Assert.Null(geschrieben);

        Speichernknopf(cut).Click();

        Assert.Equal("Versorgungssicherheit, Arbeitsschutz", geschrieben);
        Assert.Contains("gespeichert", cut.Instance.Status);
    }

    /// <summary>
    /// <b>Nach erneutem Laden steht der Text wieder da.</b> Der Prüffall geht den
    /// ganzen Weg: eingeben, speichern, den Stand neu aus der Quelle lesen lassen
    /// (wie nach einem Seitenwechsel) und wieder aufklappen.
    /// </summary>
    [Fact]
    public void Ein_gespeicherter_Text_steht_nach_erneutem_Laden_wieder_da()
    {
        WirtschaftlichkeitStand stand = Standard();
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (string t) =>
        {
            // Die Hülle schreibt und liefert den Wert beim nächsten Laden zurück.
            stand.NichtMonetaer = t;
            return true;
        }), stand: stand);

        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.Find("textarea").Input("Erfüllung einer Auflage");
        Speichernknopf(cut).Click();

        // Zuklappen und die Seite frisch laden lassen — wie nach einem Seitenwechsel.
        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.InvokeAsync(() => cut.Instance.Auffrischen());
        cut.Find("button.epos-modulparameter-knopf").Click();

        Assert.Equal("Erfüllung einer Auflage", cut.Find("textarea").TextContent);
        Assert.Equal("Erfüllung einer Auflage", cut.Instance.NichtMonetaer);
    }

    /// <summary>
    /// <b>Ein gescheitertes Schreiben SAGT es und behält die Eingabe.</b> Ein stiller
    /// Fehlschlag wäre die Behauptung, der Text stünde in der Datenbank.
    /// </summary>
    [Fact]
    public void Ein_gescheitertes_Schreiben_meldet_und_behaelt_die_Eingabe()
    {
        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (string _) => false));

        cut.Find("button.epos-modulparameter-knopf").Click();
        cut.Find("textarea").Input("Komfort");
        Speichernknopf(cut).Click();

        Assert.Single(cut.FindAll(".epos-warnbanner"));
        Assert.Equal("Komfort", cut.Instance.NichtMonetaer);
    }

    /// <summary>
    /// <b>AUFTRAG #328: Der Text steht je Zustand an genau EINER Stelle.</b>
    /// Zugeklappt weist der Kopf des Blocks ihn aus — mit dem vollen Text im
    /// <c>title</c>, damit er auch dann ganz lesbar ist, wenn der Browser
    /// abgeschnitten hat. Aufgeklappt trägt der Kopf nur seinen Titel, weil der
    /// Text dann zwei Zeilen tiefer im Feld steht.
    /// </summary>
    [Fact]
    public void Der_Ausweis_steht_im_Kopf_und_weicht_dem_offenen_Feld()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.NichtMonetaer = "Netzstabilität, Imagegewinn";

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (string _) => true), stand: stand);

        // ZUGEKLAPPT MIT TEXT: Titel und Ausweis nebeneinander, voller Text im Tooltip.
        IElement knopf = cut.Find("button.epos-modulparameter-knopf");
        Assert.Contains("Bewertung nach DIN EN 17463", knopf.TextContent);
        Assert.Equal("Netzstabilität, Imagegewinn",
                     cut.Find(".epos-modulparameter-ausweis").TextContent);
        Assert.Equal("Netzstabilität, Imagegewinn", knopf.GetAttribute("title"));

        // OFFEN: nur der Titel im Kopf, der volle Text im Feld.
        knopf.Click();

        Assert.Empty(cut.FindAll(".epos-modulparameter-ausweis"));
        Assert.Null(cut.Find("button.epos-modulparameter-knopf").GetAttribute("title"));
        Assert.Equal("Netzstabilität, Imagegewinn", cut.Find("textarea").TextContent);
    }

    /// <summary>
    /// <b>AUFTRAG #328: Leer bleibt leer.</b> Ohne gepflegten Text trägt der Kopf nur
    /// seinen Titel — kein „—", kein leeres <c>span</c> und kein leerer Tooltip. Eine
    /// leere Angabe wäre die Behauptung, es gäbe keine nicht monetären Wirkungen.
    /// </summary>
    [Fact]
    public void Ohne_gepflegten_Text_traegt_der_Kopf_nur_seinen_Titel()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.NichtMonetaer = "   ";           // auch Leerraum ist nichts Erfasstes

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (string _) => true), stand: stand);

        IElement knopf = cut.Find("button.epos-modulparameter-knopf");
        Assert.Empty(cut.FindAll(".epos-modulparameter-ausweis"));
        Assert.Null(knopf.GetAttribute("title"));

        // Im Kopf stehen genau zwei Kinder: der Pfeil und der Titel.
        Assert.Equal(2, knopf.Children.Length);
        Assert.Contains("Bewertung nach DIN EN 17463", knopf.TextContent);
    }

    /// <summary>
    /// <b>AUFTRAG #328: Im Nachweisblock steht der Text nicht mehr.</b> Er hatte dort
    /// seine fertig formulierte Zeile („Nicht monetäre Wirkungen: …"); die ist
    /// entfallen, damit derselbe Satz nicht zweimal auf einem Bildschirm steht. Die
    /// übrigen Nachweiszeilen bleiben unberührt.
    /// </summary>
    [Fact]
    public void Der_Nachweisblock_zeigt_die_Wirkungszeile_nicht_mehr()
    {
        WirtschaftlichkeitStand stand = Standard();
        stand.NichtMonetaer = "Netzstabilität, Imagegewinn";
        stand.Vereinfachungszeile = "Vereinfachungen: pauschal.";

        var cut = Zeige(p => p.Add(x => x.WirkungSpeichern, (string _) => true), stand: stand);

        string[] herleitungen = cut.FindAll("p.epos-herleitung")
                                   .Select(e => e.TextContent).ToArray();

        Assert.Contains(herleitungen, t => t.Contains("Vereinfachungen"));
        Assert.DoesNotContain(herleitungen, t => t.Contains("Netzstabilität"));
    }

    /// <summary>Der Speichernknopf des Bewertungsblocks — er steht ohne Leiste da,
    /// damit die Seite genau EINE Fussleiste behält.</summary>
    private static IElement Speichernknopf(IRenderedComponent<WirtschaftlichkeitSeite> cut)
        => cut.FindAll(".epos-seite > button.epos-knopf")
              .First(k => k.TextContent.Trim() == "Speichern");

    // =====================================================================
    // Unterdialoge in der Überlagerung
    // =====================================================================

    [Theory]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Strombezug)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Verlauf)]
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
    /// </summary>
    [Theory]
    [InlineData(BhkwSprung.BhkwTarif, WirtschaftlichkeitSeite.Unterdialog.TarifBhkw)]
    [InlineData(BhkwSprung.Strombezug, WirtschaftlichkeitSeite.Unterdialog.Strombezug)]
    public void Der_Bhkw_Sprung_oeffnet_die_Tarifstruktur_in_seiner_Sicht(
        BhkwSprung sprung, WirtschaftlichkeitSeite.Unterdialog erwartet)
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
            new BhkwWirtschaftlichkeitErgebnis(true, sprung)));

        Assert.Equal(erwartet, cut.Instance.OffenerUnterdialog);
        Assert.Equal(erwartet, gefragt[^1]);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
        Assert.Single(cut.FindComponents<TarifstrukturDialog>());
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
    /// 15.09.2026): Jede der fünf Überlagerungen trägt Titel UND Kreuz, der
    /// eingebettete Dialog darin keins von beiden — die vier Bauart-b-Dialoge über
    /// <c>TitelAnzeigen="false"</c>, der Kapitalwert-Verlauf über <c>TitelText=""</c>;
    /// beide stehen RECHTS vom Parametersatz der Hülle und gelten deshalb auch dann,
    /// wenn dieser einen Titel mitbrächte.
    /// </summary>
    [Theory]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Strombezug)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Verlauf)]
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
    /// darin zeigt keinen eigenen Kopf — und zwar JEDER der fünf. Die vier
    /// Geschwister des BHKW-Dialogs beziehen ihren Titel aus einem eigenen
    /// Ausdruck; deshalb sah die Markup-Wache sie bis #289 nicht, und deshalb
    /// steht hier der GEZEICHNETE Nachweis: genau ein Titel je Bereich, der der
    /// Überlagerung.
    /// </summary>
    [Theory]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Strombezug)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    [InlineData(WirtschaftlichkeitSeite.Unterdialog.Verlauf)]
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

        // Der Freitext der nicht monetaeren Wirkungen ist ein Feld dieser Seite.
        KiFeldzugang wirkung = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE, "nicht_monetaer");
        Assert.NotNull(wirkung);
        Assert.True(wirkung.Setzbar);

        // Die Vergleichsgruppe ist eine Menge von Verweisen und KEIN Feld.
        Assert.Null(KiDialoge.Katalog.Finde(KiMaskennamen.WIRTSCHAFTLICHKEITSSEITE)!
                              .FindeFeld("gewaehlte_varianten"));
    }
}
