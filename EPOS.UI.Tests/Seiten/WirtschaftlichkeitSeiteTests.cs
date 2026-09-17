using AngleSharp.Dom;
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
/// mit Haken, die Szenariowahl, der Parameternachweis, die Vergleichstabelle,
/// die Sicht-Knöpfe (Photovoltaik, BHKW, Strombezug — je nach Ausstattung),
/// „Parameter…", „Verlauf…", „Berechnen" und der Abbrechen-Knopf während
/// eines Laufs.</para>
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

    [Fact]
    public void Die_drei_Sichtknoepfe_folgen_der_Ausstattung()
    {
        var alle = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));
        Assert.Equal(6, Fussknoepfe(alle).Count);   // PV, BHKW, Strom, Parameter, Verlauf, Berechnen

        var ohne = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()),
                         stand: Standard(pv: false, bhkw: false, strom: false));
        Assert.Equal(3, Fussknoepfe(ohne).Count);   // Parameter, Verlauf, Berechnen
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
    // Unterdialoge in der Überlagerung
    // =====================================================================

    [Theory]
    [InlineData(0, WirtschaftlichkeitSeite.Unterdialog.Photovoltaik)]
    [InlineData(1, WirtschaftlichkeitSeite.Unterdialog.Bhkw)]
    [InlineData(2, WirtschaftlichkeitSeite.Unterdialog.Strombezug)]
    [InlineData(3, WirtschaftlichkeitSeite.Unterdialog.Parameter)]
    [InlineData(4, WirtschaftlichkeitSeite.Unterdialog.Verlauf)]
    public void Jeder_Sichtknopf_oeffnet_seinen_Bereich(
        int knopf, WirtschaftlichkeitSeite.Unterdialog erwartet)
    {
        WirtschaftlichkeitSeite.Unterdialog gefragt = WirtschaftlichkeitSeite.Unterdialog.Keins;
        var cut = Zeige(p => p.Add(x => x.Gaben, (WirtschaftlichkeitSeite.Unterdialog a) =>
        {
            gefragt = a;
            return LeererSatz();
        }));

        Fussknoepfe(cut)[knopf].Click();

        Assert.Equal(erwartet, gefragt);
        Assert.Equal(erwartet, cut.Instance.OffenerUnterdialog);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
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
    [InlineData(0)]   // Photovoltaik
    [InlineData(1)]   // BHKW
    [InlineData(2)]   // Strombezug
    [InlineData(3)]   // Parameter
    [InlineData(4)]   // Kapitalwert-Verlauf
    public void Jede_Ueberlagerung_zeigt_nur_ein_Kreuz_und_einen_Titel(int knopf)
    {
        var cut = Zeige(p => p.Add(x => x.Gaben,
            (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Fussknoepfe(cut)[knopf].Click();

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

        Fussknoepfe(cut)[3].Click();                          // Parameter
        // Esc auf der Ueberlagerung schliesst ohne zu speichern.
        cut.Find(".epos-ueberlagerung").KeyDown("Escape");

        Assert.Equal(WirtschaftlichkeitSeite.Unterdialog.Keins, cut.Instance.OffenerUnterdialog);
        Assert.Equal(2, _geladen);                            // Aufbau + Nachlauf
    }

    /// <summary>
    /// Auftrag #286, Einbettungsstelle 2: Der BHKW-Dialog schreibt auch hier nur im
    /// OK-Weg. Nach OK bekommt der Wirt seine Speichermeldung und rechnet neu, nach
    /// Abbrechen bekommt er nichts — und die Datenbank sieht keinen Zugriff.
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
        Assert.Equal(ok ? 2 : 0, zugriffe);
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
    [InlineData(0)]   // Photovoltaik
    [InlineData(1)]   // BHKW
    [InlineData(2)]   // Strombezug
    [InlineData(3)]   // Parameter
    [InlineData(4)]   // Verlauf
    public void Kein_Unterdialog_zeigt_in_der_Ueberlagerung_einen_eigenen_Titel(int knopf)
    {
        var cut = Zeige(p => p.Add(x => x.Gaben,
            (WirtschaftlichkeitSeite.Unterdialog _) => LeererSatz()));

        Fussknoepfe(cut)[knopf].Click();

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

        Fussknoepfe(cut)[3].Click();                          // Parameter
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
}
