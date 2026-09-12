using System.Globalization;
using System.IO;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die Seite „Übersicht" (iU9-W5.5), Vorbild
/// <c>Views/BerichteKosten/UcBkUebersicht</c> (1 552 Z., K4).
///
/// <para>Soll: Stammprojekt-Auswahl und Filter, die VERSIONSWAHL als
/// Auswahlfeld samt Simulationszeile, Bezeichnerfeld und die drei Knöpfe, der
/// Komponentenbereich in beiden Ansichten (Gegenüberstellung ohne, Unterschiede
/// mit Aktionsspalte) und die Statuszeile.</para>
///
/// <para><b>Anwenderwunsch 05.09.2026 (W5‑E‑1)</b> — „Variantenprojekte-Auswahl
/// als Dropdown, damit weniger Platz verwendet wird": Die Variantentabelle mit
/// Wahlknopf, Art, Bezeichner, Projektname und Simulationsspalte ist einem
/// <c>Auswahlfeld</c> gewichen; der Simulationsstand steht als leise Zeile
/// darunter, die Unterschiedstabelle bekommt die frei gewordene Höhe.</para>
/// </summary>
public class UebersichtSeiteTests : EposBunitContext
{
    public UebersichtSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // ---- Probendaten -----------------------------------------------------

    private static VarianteZeile[] Zeilen() => new[]
    {
        new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                            Projektname = "Musterhaus", SimStand = "02.09.26 10:00",
                            SimZeitpunkt = "02.09.26 10:00", IstStamm = true },
        new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "WP klein",
                            Projektname = "Musterhaus - WP klein", SimStand = "— (fehlt) ⚠",
                            SimZeitpunkt = "", Auffaellig = true }
    };

    /// <summary>Die Gegenüberstellung (Stammzeile markiert) — ohne Aktionsspalte.</summary>
    private static UebersichtStand Vergleichsansicht() => new UebersichtStand
    {
        Staemme = new[] { (1030, "Musterhaus"), (1040, "Bürohaus") },
        StammId = 1030,
        Zeilen = Zeilen(),
        MarkierteId = 1030,
        GewaehlteVarianten = new[] { 1030, 1031 },
        KomponentenTitel = "Komponenten der Gruppe im Vergleich",
        Spalten = new[] { "Gewerk", "Merkmal", "Stamm", "WP klein" },
        Vergleich = new[]
        {
            new VergleichZeile { Gewerk = "Wärmepumpe", Merkmal = "Anzahl Komponenten",
                                 Zellen = new[] { "1", "2" } },
            new VergleichZeile { Gewerk = "", Merkmal = "Komponente 1",
                                 Zellen = new[] { "WP 1", "WP klein 1" },
                                 Kurztexte = new[] { "Hersteller: A", "Hersteller: B" } }
        },
        AnlegenMoeglich = true,
        SimulierenMoeglich = true,
        Statuszeile = "2 Zeile(n) im Vergleich über 1 Variante(n)."
    };

    /// <summary>Die Unterschiede (Variantenzeile markiert) — mit Aktionsspalte.</summary>
    private static UebersichtStand Unterschiedsansicht() => new UebersichtStand
    {
        Staemme = new[] { (1030, "Musterhaus") },
        StammId = 1030,
        Zeilen = Zeilen(),
        MarkierteId = 1031,
        KomponentenTitel = "Unterschiede der Variante „WP klein“",
        Spalten = new[] { "Gewerk", "Merkmal", "Stamm", "Variante" },
        Vergleich = new[]
        {
            new VergleichZeile { Schluessel = 7, Gewerk = "Wärmepumpe", Merkmal = "Nennleistung",
                                 Zellen = new[] { "12,5", "9,0" }, MitAktion = true,
                                 AktionKurztext = "Dieses Feld aus einer anderen Version übernehmen" },
            new VergleichZeile { Schluessel = 8, Gewerk = "", Merkmal = "Bezeichner",
                                 Zellen = new[] { "WP 1", "WP 2" }, MitAktion = true,
                                 Sperrgrund = "Der Bezeichner ist die Schlüsselspalte." }
        },
        Loeschbar = true,
        AnlegenMoeglich = true,
        SimulierenMoeglich = true
    };

    private UebersichtStand _stand = Vergleichsansicht();
    private int _geladen;

    private IRenderedComponent<UebersichtSeite> Zeige(
        Action<Bunit.ComponentParameterCollectionBuilder<UebersichtSeite>>? mehr = null,
        UebersichtStand? stand = null)
    {
        _stand = stand ?? Vergleichsansicht();
        _geladen = 0;
        return Render<UebersichtSeite>(p =>
        {
            p.Add(x => x.Laden, () => { _geladen++; return _stand; });
            mehr?.Invoke(p);
        });
    }

    private static IReadOnlyDictionary<string, object> LeererSatz()
        => new Dictionary<string, object>();

    /// <summary>Das Auswahlfeld der Versionen (seit W5‑E‑1 statt der Tabelle).</summary>
    private static IElement Versionswahl(IRenderedComponent<UebersichtSeite> cut)
        => cut.Find(".epos-variantenzeile select");

    private static IReadOnlyList<IElement> Versionseintraege(IRenderedComponent<UebersichtSeite> cut)
        => Versionswahl(cut).QuerySelectorAll("option");

    private static IElement Simulationszeile(IRenderedComponent<UebersichtSeite> cut)
        => cut.Find(".epos-simstand");

    private static IReadOnlyList<IElement> Vergleichszeilen(IRenderedComponent<UebersichtSeite> cut)
        => cut.Find(".epos-vergleichstabelle tbody").QuerySelectorAll("tr");

    private static IReadOnlyList<IElement> Pflegeknoepfe(IRenderedComponent<UebersichtSeite> cut)
        => cut.Find(".epos-variantenzeile").QuerySelectorAll("button");

    /// <summary>
    /// Ein Pflegeknopf über seine BESCHRIFTUNG (Auftrag #240). Bis dahin zählten die
    /// Fälle Stellen — und seit „Variante anlegen" nur noch mit Öffner erscheint
    /// (Hausregel A‑18), wandert jede Stelle mit dem Parametersatz. Der Text ist das,
    /// was auch der Anwender sieht.
    /// </summary>
    private static IElement Knopf(IRenderedComponent<UebersichtSeite> cut, string text)
        => Pflegeknoepfe(cut).First(b => b.TextContent.Trim() == text);

    private static IElement Anlegenknopf(IRenderedComponent<UebersichtSeite> cut)
        => Knopf(cut, "Variante anlegen");

    private static IElement Loeschknopf(IRenderedComponent<UebersichtSeite> cut)
        => Knopf(cut, "Variante löschen");

    private static IElement Simulierknopf(IRenderedComponent<UebersichtSeite> cut)
        => Knopf(cut, "Simulation starten");

    private static IElement Umbenennknopf(IRenderedComponent<UebersichtSeite> cut)
        => Knopf(cut, "Umbenennen");

    // =====================================================================
    // Feldbestand
    // =====================================================================

    [Fact]
    public void Die_Seite_zeigt_Auswahl_Filter_Versionswahl_Pflege_und_Vergleich()
    {
        var cut = Zeige();

        Assert.Equal(2, cut.FindAll("select").Count);                   // Stammprojekt, Version
        Assert.Single(cut.FindAll(".epos-stammzeile input[type=checkbox]"));  // nur Stämme
        Assert.Equal(2, Versionseintraege(cut).Count);

        // Seit #240: Das Bezeichnerfeld gehoert dem UMBENENNEN, und "Variante anlegen"
        // erscheint nur mit Oeffner - ohne beide Delegaten bleiben zwei Knoepfe.
        Assert.Empty(cut.FindAll(".epos-variantenzeile input[type=text]"));
        Assert.Equal(2, Pflegeknoepfe(cut).Count);                      // Löschen, Simulieren
        Assert.Contains("im Vergleich", cut.Find(".epos-untergruppe").TextContent);
        Assert.Contains("2 Zeile(n)", cut.Find(".epos-status").TextContent);
    }

    /// <summary>
    /// W5‑E‑1: Die Variantentabelle ist WEG — mit ihr die fünf Spaltenköpfe, die
    /// zwei Wahlknöpfe und die vier Zeilenzellen je Version. Ein Rest von ihr wäre
    /// genau der Platz, den der Anwender zurückhaben wollte.
    /// </summary>
    [Fact]
    public void Die_Variantentabelle_gibt_es_nicht_mehr()
    {
        var cut = Zeige();

        Assert.Empty(cut.FindAll(".epos-variantentabelle"));
        Assert.Empty(cut.FindAll(".epos-variantenpflege"));
        Assert.Empty(cut.FindAll(".epos-anlagenwahl"));      // die Zeilenwahl der Liste
    }

    [Fact]
    public void Die_Gegenueberstellung_traegt_keine_Aktionsspalte()
    {
        var cut = Zeige();

        Assert.False(cut.Instance.MitAktionsspalte);
        Assert.Equal(4, cut.FindAll(".epos-vergleichstabelle thead th").Count);
        Assert.Empty(cut.FindAll(".epos-vergleichstabelle .epos-zellenaktionen"));
    }

    [Fact]
    public void Die_Unterschiedsansicht_traegt_die_Aktionsspalte()
    {
        var cut = Zeige(p => p.Add(x => x.UebernahmeGaben, (VergleichZeile _) => LeererSatz()),
                        stand: Unterschiedsansicht());

        Assert.True(cut.Instance.MitAktionsspalte);
        Assert.Equal(5, cut.FindAll(".epos-vergleichstabelle thead th").Count);
        Assert.Equal(2, cut.FindAll(".epos-vergleichstabelle .epos-zellenaktionen").Count);
    }

    /// <summary>
    /// „Ein Knopf, der beim Drücken nur erklärt, warum er nichts tut, wäre die
    /// schlechtere Auskunft" — der Vorläufer setzte einen grauen Strich.
    /// </summary>
    [Fact]
    public void Eine_gesperrte_Zeile_zeigt_den_Strich_mit_Begruendung()
    {
        var cut = Zeige(p => p.Add(x => x.UebernahmeGaben, (VergleichZeile _) => LeererSatz()),
                        stand: Unterschiedsansicht());

        var zeilen = Vergleichszeilen(cut);
        Assert.Single(zeilen[0].QuerySelectorAll(".epos-zellenaktionen button"));
        Assert.Empty(zeilen[1].QuerySelectorAll(".epos-zellenaktionen button"));

        var strich = zeilen[1].QuerySelector(".epos-gesperrt")!;
        Assert.Equal("—", strich.TextContent);
        Assert.Contains("Schlüsselspalte", strich.GetAttribute("title"));
    }

    [Fact]
    public void Das_Gewerk_steht_nur_in_der_ersten_Zeile_seines_Blocks()
    {
        var cut = Zeige();

        var zeilen = Vergleichszeilen(cut);
        Assert.Equal("Wärmepumpe", zeilen[0].QuerySelector(".epos-vergleich-gewerk")!.TextContent);
        Assert.Equal("", zeilen[1].QuerySelector(".epos-vergleich-gewerk")!.TextContent);
    }

    [Fact]
    public void Die_Merkmale_einer_Komponente_stehen_als_Kurztext_an_der_Zelle()
    {
        var cut = Zeige();

        var zellen = Vergleichszeilen(cut)[1].QuerySelectorAll("td");
        Assert.Equal("Hersteller: A", zellen[1].GetAttribute("title"));
        Assert.Equal("Hersteller: B", zellen[2].GetAttribute("title"));
    }

    // =====================================================================
    //  Anwenderbefund W5‑E‑2 (05.09.2026) — die Gegenüberstellung zeigt
    //  ausschließlich verwendete ERZEUGERKOMPONENTEN
    // =====================================================================

    /// <summary>
    /// Der Stand des Bildschirmfotos vom 05.09.2026 — Projekt „Booster-Kette mit
    /// Kombi-Speicher" (1042) mit seiner Variante „Schichtspeicher" (1044), so wie
    /// ihn <c>KomponentenVergleich.Gegenueberstellung</c> seither baut: zwei
    /// Wärmepumpen, ein Spitzenkessel, vier Pufferspeicher — und kein Gewerk
    /// „Anlage".
    /// </summary>
    private static UebersichtStand ErzeugeransichtW5E2() => new UebersichtStand
    {
        Staemme = new[] { (1042, "Booster-Kette mit Kombi-Speicher") },
        StammId = 1042,
        Zeilen = Zeilen(),
        MarkierteId = 1030,
        KomponentenTitel = "Komponenten im Vergleich — Stammprojekt und Varianten",
        Spalten = new[] { "Gewerk", "Merkmal", "Stamm", "Schichtspeicher" },
        Vergleich = new[]
        {
            new VergleichZeile { Gewerk = "Wärmepumpe", Merkmal = "Anzahl Komponenten",
                                 Zellen = new[] { "2", "2" } },
            new VergleichZeile { Merkmal = "Komponente 1",
                                 Zellen = new[] { "CS6800iAW", "CS6800iAW" } },
            new VergleichZeile { Merkmal = "Komponente 2",
                                 Zellen = new[] { "CS7800iLW 16", "CS7800iLW 16" } },
            new VergleichZeile { Gewerk = "Spitzenkessel", Merkmal = "Anzahl Komponenten",
                                 Zellen = new[] { "1", "1" } },
            new VergleichZeile { Merkmal = "Komponente",
                                 Zellen = new[] { "ecoTEC plus", "ecoTEC plus" } },
            new VergleichZeile { Gewerk = "Pufferspeicher", Merkmal = "Anzahl Komponenten",
                                 Zellen = new[] { "4", "4" } }
        },
        Statuszeile = "6 Komponentenzeile(n) für das Stammprojekt und 1 Variante(n)."
    };

    /// <summary>
    /// Die Wache zum Anwenderbefund W5‑E‑2: „Gewerk Anlage gibt es nicht. Dort
    /// stehen Parameter. Dargestellt werden nur die Erzeugerkomponenten, die
    /// verwendet werden, keine Parameter." Die Zeilenbildung dazu steht im Kern
    /// (<c>KomponentenVergleich</c>, Proben V1–V6); hier wird geprüft, dass die
    /// SEITE genau diese Zeilen zeigt — mit dem Gewerk je Blockanfang und ohne
    /// Aktionsspalte.
    /// </summary>
    [Fact]
    public void W5E2_Die_Gegenueberstellung_zeigt_nur_Erzeugerkomponenten()
    {
        var cut = Zeige(stand: ErzeugeransichtW5E2());

        var zeilen = Vergleichszeilen(cut);
        Assert.Equal(6, zeilen.Count);

        // Die Gewerkspalte trägt die drei verwendeten Erzeugergewerke — und nichts
        // sonst. Ein „Anlage" hier wäre der Rückfall in den Befund.
        var gewerke = zeilen.Select(z => z.QuerySelector(".epos-vergleich-gewerk")!.TextContent)
                            .Where(t => t.Length > 0).ToArray();
        Assert.Equal(new[] { "Wärmepumpe", "Spitzenkessel", "Pufferspeicher" }, gewerke);

        // Kein Anlagenparameter steht mehr in der Merkmalspalte.
        string tabelle = cut.Find(".epos-vergleichstabelle").TextContent;
        foreach (string parameter in new[] { "Anlage", "Vorlauftemperatur", "Rücklauftemperatur",
                                             "Abschaltpunkt", "Neigung", "Azimut", "Solaranteil" })
            Assert.DoesNotContain(parameter, tabelle);

        // Die Gegenüberstellung trägt keine Aktionsspalte (die gehört den
        // Unterschieden), also vier Spalten wie im Bildschirmfoto.
        Assert.False(cut.Instance.MitAktionsspalte);
        Assert.Equal(4, cut.Find(".epos-vergleichstabelle thead tr").QuerySelectorAll("th").Count);
    }

    // =====================================================================
    //  Die Versionswahl (Anwenderwunsch 05.09.2026, W5‑E‑1)
    // =====================================================================

    /// <summary>
    /// Der Stamm steht zuerst, dann die Varianten; der Text ist der PROJEKTNAME —
    /// derselbe, den das Auswahlfeld „Projekt:" im Kopfband der Startseite zeigt.
    /// Bis zum Anwenderbefund vom 12.09.2026 (<b>Auftrag #238</b>) stand hier
    /// „Bezeichner — Projektname"; weil der Projektname einer Variante selbst
    /// „&lt;Stamm&gt; - &lt;Bezeichner&gt;" ist, stand der Bezeichner DOPPELT im
    /// Eintrag (z. B. „ein Speicher — Stromspeicher Optimierung - ein Speicher").
    /// Die Ids sind die Projekt-Ids (stabil über einen Neuaufbau, dublettenfrei —
    /// die Bedingung der Hausregel für den Wirt eines <c>Auswahlfeld</c>).
    /// </summary>
    [Fact]
    public void Das_Auswahlfeld_fuehrt_Stamm_und_Varianten_in_der_Reihenfolge_der_Gruppe()
    {
        var cut = Zeige();

        IReadOnlyList<IElement> eintraege = Versionseintraege(cut);
        Assert.Equal(2, eintraege.Count);

        Assert.Equal("1030", eintraege[0].GetAttribute("value"));
        Assert.Equal("Musterhaus", eintraege[0].TextContent);

        Assert.Equal("1031", eintraege[1].GetAttribute("value"));
        Assert.Equal("Musterhaus - WP klein", eintraege[1].TextContent);

        // Der Bezeichner "WP klein" steht in der Variantenzeile nur EINMAL - als
        // Teil des Projektnamens, nicht zusaetzlich davor mit einem Trennzeichen.
        Assert.DoesNotContain(" — ", eintraege[1].TextContent);
    }

    /// <summary>
    /// Das genaue Anwenderbeispiel aus Auftrag #238 (Bildschirmfoto „Berichte &amp;
    /// Kosten › Übersicht", Projekt „Stromspeicher Optimierung"): Der Eintrag der
    /// Variante „ein Speicher" lautete vorher „ein Speicher — Stromspeicher
    /// Optimierung - ein Speicher", der Stamm „(Stammprojekt) — Stromspeicher
    /// Optimierung". Beide Einträge sind seither schlicht der Projektname.
    /// </summary>
    [Fact]
    public void Das_Anwenderbeispiel_aus_Auftrag_238_zeigt_nur_noch_den_Projektnamen()
    {
        var stand = new UebersichtStand
        {
            Zeilen = new[]
            {
                new VarianteZeile { IdProjekt = 2000, Bezeichner = "(Stammprojekt)",
                                     Projektname = "Stromspeicher Optimierung", IstStamm = true },
                new VarianteZeile { IdProjekt = 2001, Bezeichner = "ein Speicher",
                                     Projektname = "Stromspeicher Optimierung - ein Speicher" },
                new VarianteZeile { IdProjekt = 2002, Bezeichner = "Stromspeicher mit Wärmepumpe",
                                     Projektname = "Stromspeicher Optimierung - Stromspeicher mit Wärmepumpe" }
            },
            MarkierteId = 2000
        };

        IReadOnlyList<IElement> eintraege = Versionseintraege(Zeige(stand: stand));

        Assert.Equal("Stromspeicher Optimierung", eintraege[0].TextContent);
        Assert.Equal("Stromspeicher Optimierung - ein Speicher", eintraege[1].TextContent);
        Assert.Equal("Stromspeicher Optimierung - Stromspeicher mit Wärmepumpe", eintraege[2].TextContent);
    }

    /// <summary>
    /// Fehlt der Projektname (er ist im Datenmodell nicht Pflicht), bleibt der
    /// Rückfall auf den Bezeichner — wie bisher.
    /// </summary>
    [Fact]
    public void Ohne_Projektname_faellt_der_Eintrag_auf_den_Bezeichner_zurueck()
    {
        var stand = new UebersichtStand
        {
            Zeilen = new[]
            {
                new VarianteZeile { IdProjekt = 3000, Bezeichner = "Ohne Projektname",
                                     Projektname = "", IstStamm = true }
            },
            MarkierteId = 3000
        };

        Assert.Equal("Ohne Projektname", Versionseintraege(Zeige(stand: stand))[0].TextContent);
    }

    /// <summary>Gewählt ist, was der Stand als markiert meldet.</summary>
    [Fact]
    public void Das_Auswahlfeld_zeigt_die_markierte_Version()
    {
        Assert.Equal("1030", Versionswahl(Zeige()).GetAttribute("value"));
        Assert.Equal("1031", Versionswahl(Zeige(stand: Unterschiedsansicht())).GetAttribute("value"));
    }

    /// <summary>
    /// Die Variantenzeile (Auswahlfeld, Bezeichner, die Knöpfe) bricht bei
    /// Platzmangel um, statt dass sich das Auswahlfeld, seine aufgeklappte Liste
    /// und das Bezeichnerfeld überlappen (Anwenderrückmeldung 12.09.2026, Auftrag
    /// #238, „korrigiere dabei auch die Überlappung"). Geprüft wird — wie in
    /// <see cref="Die_Unterschiedstabelle_steht_im_hoeheren_Rahmen"/> — beides:
    /// das MARKUP (die Zeilen tragen die Klasse, die im Stilblatt den Umbruch
    /// setzt) UND die REGEL selbst (eine bunit-Probe allein sieht eine Stilregel
    /// nicht, Lehre W6‑B‑1). Keine Pixelmessung — die gehört in
    /// <c>Proben/Rasterprobe</c>, nicht hierher.
    /// </summary>
    [Fact]
    public void Varianten_und_Stammzeile_tragen_die_Umbruchklasse_und_das_Auswahlfeld_hat_keine_Hoechstbreite()
    {
        var cut = Zeige();

        Assert.Contains("epos-seite-zeile", cut.Find(".epos-variantenzeile").ClassName);
        Assert.Contains("epos-seite-zeile", cut.Find(".epos-stammzeile").ClassName);

        string zeilenregel = Stilblock(".epos-seite-zeile {");
        Assert.Contains("flex-wrap: wrap", zeilenregel);

        string feldregel = Stilblock(".epos-variantenzeile > .epos-feld:first-child {");
        Assert.DoesNotContain("max-width", feldregel);
    }

    /// <summary>
    /// Ein Auswahlfeld ohne Eintrag zu seinem Wert zeigt NICHTS an (Hausregel).
    /// Ohne Gruppe gibt es beides nicht — und die Seite zeichnet trotzdem.
    /// </summary>
    [Fact]
    public void Ohne_Gruppe_bleibt_das_Auswahlfeld_leer_und_die_Seite_steht()
    {
        var cut = Zeige(stand: new UebersichtStand());

        Assert.Empty(Versionseintraege(cut));
        Assert.Equal("", Versionswahl(cut).GetAttribute("value"));
        Assert.Equal("", Simulationszeile(cut).TextContent.Trim());
    }

    /// <summary>Für die Sprachausgabe trägt das Feld einen ausgeschriebenen Namen.</summary>
    [Fact]
    public void Das_Auswahlfeld_traegt_einen_Namen_fuer_die_Sprachausgabe()
    {
        var cut = Zeige();

        Assert.Equal("Version wählen", Versionswahl(cut).GetAttribute("aria-label"));
        Assert.Contains("Variante:", cut.Find(".epos-variantenzeile .epos-feld-text").TextContent);
    }

    // =====================================================================
    //  Die Statuszeile des Simulationsstands
    // =====================================================================

    /// <summary>
    /// Ein gerechneter, aktueller Stand: „Simulation: 02.09.26 10:00" — ohne „⚠".
    /// Die Zeile meldet sich der Sprachausgabe von selbst (<c>aria-live</c>), denn
    /// sie wechselt mit der Version, ohne dass der Anwender hinsieht.
    /// </summary>
    [Fact]
    public void Die_Statuszeile_nennt_den_Simulationsstand_der_gewaehlten_Version()
    {
        var cut = Zeige();

        IElement zeile = Simulationszeile(cut);
        Assert.Equal("polite", zeile.GetAttribute("aria-live"));
        Assert.Contains("Simulation: 02.09.26 10:00", zeile.TextContent);
        Assert.Empty(zeile.QuerySelectorAll(".epos-veraltet"));
    }

    /// <summary>Ohne Ergebnis: „noch nicht simuliert" samt „⚠" und Grund im Kurztext.</summary>
    [Fact]
    public void Ohne_Ergebnis_sagt_die_Statuszeile_es_und_traegt_das_Warnzeichen()
    {
        var cut = Zeige(stand: Unterschiedsansicht());

        IElement zeile = Simulationszeile(cut);
        Assert.Contains("noch nicht simuliert", zeile.TextContent);

        IElement warn = zeile.QuerySelector(".epos-veraltet")!;
        Assert.Equal("⚠", warn.TextContent);
        Assert.Contains("kein Simulationsergebnis", warn.GetAttribute("title"));
    }

    /// <summary>
    /// Das „⚠" hat ZWEI Bedeutungen — kein Ergebnis, oder ein Ergebnis, das älter
    /// ist als die letzte Änderung am Projekt (<c>SimStand &lt; Aenderungsdatum</c>
    /// in <c>BerichtsDatenSammler.ErmittleStatus</c>). Der Kurztext sagt, welche
    /// gerade gilt; bis W5‑E‑1 sagte es nichts.
    /// </summary>
    [Fact]
    public void Ein_veralteter_Stand_zeigt_Zeitpunkt_Warnzeichen_und_den_zweiten_Grund()
    {
        UebersichtStand stand = Vergleichsansicht();
        stand.Zeilen = new[]
        {
            new VarianteZeile { IdProjekt = 1030, Bezeichner = "(Stammprojekt)",
                                Projektname = "Musterhaus", SimStand = "02.09.26 10:00 ⚠",
                                SimZeitpunkt = "02.09.26 10:00", IstStamm = true,
                                Auffaellig = true }
        };
        var cut = Zeige(stand: stand);

        IElement zeile = Simulationszeile(cut);
        Assert.Contains("Simulation: 02.09.26 10:00", zeile.TextContent);

        IElement warn = zeile.QuerySelector(".epos-veraltet")!;
        Assert.Equal("⚠", warn.TextContent);
        Assert.Contains("älter als die letzte Änderung", warn.GetAttribute("title"));
    }

    /// <summary>
    /// Die Unterschiedstabelle steht weiter im Rahmen der Hausregel W9‑B‑2 —
    /// nur mit der größeren Höchsthöhe, die die Variantentabelle frei gemacht hat.
    /// Geprüft wird beides: das Markup UND die Regel im Stilblatt (eine bunit-Probe
    /// allein sieht eine Stilregel nicht, Lehre W6‑B‑1).
    /// </summary>
    [Fact]
    public void Die_Unterschiedstabelle_steht_im_hoeheren_Rahmen()
    {
        var cut = Zeige();

        string klasse = cut.Find(".epos-vergleichstabelle").ParentElement!.ClassName ?? "";
        Assert.Contains("epos-raster-huelle", klasse);
        Assert.Contains("epos-raster-huelle--vergleich", klasse);
        Assert.DoesNotContain("epos-raster-huelle--frei", klasse);

        string regel = Stilblock(".epos-raster-huelle--vergleich {");
        Assert.Contains("max-height:", regel);
        Assert.Contains("--epos-listenhoehe", regel);
    }

    // =====================================================================
    // Auswahl
    // =====================================================================

    [Fact]
    public void Ein_Stammwechsel_wird_gemeldet_und_laedt_neu()
    {
        int gemeldet = 0;
        var cut = Zeige(p => p.Add(x => x.StammGewechselt, (int id) => gemeldet = id));

        cut.Find(".epos-stammzeile select").Change("1040");

        Assert.Equal(1040, gemeldet);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Der_Filter_wird_gemeldet_und_laedt_neu()
    {
        bool? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.FilterGewechselt, (bool an) => gemeldet = an));

        cut.Find(".epos-stammzeile input[type=checkbox]").Change(true);

        Assert.True(gemeldet);
        Assert.Equal(2, _geladen);
    }

    /// <summary>
    /// W5‑E‑1: Die Wahl im Auswahlfeld treibt genau das, was bis dahin die
    /// Zeilenwahl der Tabelle trieb — dieselbe Meldung, dasselbe Auffrischen.
    /// </summary>
    [Fact]
    public void Eine_Versionswahl_wird_gemeldet_und_laedt_neu()
    {
        int gemeldet = 0;
        var cut = Zeige(p => p.Add(x => x.ZeileMarkiert, (int id) => gemeldet = id));

        Versionswahl(cut).Change("1031");

        Assert.Equal(1031, gemeldet);
        Assert.Equal(2, _geladen);
    }

    /// <summary>
    /// Und sie treibt die ANSICHT: Auf dem Stamm steht die Gegenüberstellung ohne
    /// Aktionsspalte, auf einer Variante deren Unterschiede mit ihr — dazu die
    /// Löschsperre und die Statuszeile des neuen Stands.
    /// </summary>
    [Fact]
    public void Eine_Versionswahl_treibt_MarkierteId_Unterschiede_und_Knoepfe()
    {
        var cut = Zeige(p => p
            .Add(x => x.ZeileMarkiert, (int _) => _stand = Unterschiedsansicht())
            .Add(x => x.UebernahmeGaben, (VergleichZeile _) => LeererSatz()));

        Assert.Equal(1030, cut.Instance.MarkierteId);
        Assert.False(cut.Instance.MitAktionsspalte);
        Assert.True(Loeschknopf(cut).HasAttribute("disabled"));           // Stamm: nicht löschbar

        Versionswahl(cut).Change("1031");

        Assert.Equal(1031, cut.Instance.MarkierteId);
        Assert.True(cut.Instance.MitAktionsspalte);
        Assert.Contains("WP klein", cut.Find(".epos-untergruppe").TextContent);
        Assert.False(Loeschknopf(cut).HasAttribute("disabled"));          // Variante: löschbar
        Assert.Contains("noch nicht simuliert", Simulationszeile(cut).TextContent);
    }

    // =====================================================================
    // Variante anlegen und löschen
    // =====================================================================

    /// <summary>Umbenennen (08.09.2026): der Bezeichner aus dem Feld geht an die Hülle, die Meldung kommt zurück.</summary>
    [Fact]
    public void Umbenennen_gibt_den_Bezeichner_weiter_und_meldet()
    {
        string? bezeichner = null;
        var cut = Zeige(p => p.Add(x => x.VarianteUmbenennen, (string b) =>
        {
            bezeichner = b;
            return "Die Variante „V2“ heißt jetzt „Kessel klein“.";
        }));

        cut.Find(".epos-variantenzeile input[type=text]").Input("Kessel klein");
        Assert.Equal(3, Pflegeknoepfe(cut).Count);      // Löschen, Simulieren, Umbenennen
        Umbenennknopf(cut).Click();

        Assert.Equal("Kessel klein", bezeichner);
        Assert.Contains("heißt jetzt", cut.Instance.Status);
    }

    /// <summary>
    /// <b>EIN Dialog, drei Einstiege</b> (Auftrag <b>#240</b>): Der Knopf „Variante
    /// anlegen" ÖFFNET nur — denselben <c>ProjektVarianteDialog</c>, den Kopfband und
    /// Menüpunkt seit #237 zeigen. Meldet der Öffner <c>true</c>, lädt die Seite ihre
    /// Variantenliste neu; den Bezeichner fragt der Dialog selbst, das inline Feld ist
    /// dafür gefallen.
    /// </summary>
    [Fact]
    public void Anlegen_oeffnet_den_Dialog_und_laedt_danach_neu()
    {
        int geoeffnet = 0;
        var cut = Zeige(p => p.Add(x => x.VarianteAnlegenOeffnen,
                                   () => { geoeffnet++; return Task.FromResult(true); }));

        Anlegenknopf(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(2, _geladen), TimeSpan.FromSeconds(10));
        Assert.Equal(1, geoeffnet);
    }

    /// <summary>
    /// GEGENPROBE: Bricht der Anwender den Dialog ab (<c>false</c>), bleibt die Liste
    /// stehen — ein Neuladen ohne Änderung wäre ein Flackern ohne Aussage.
    /// </summary>
    [Fact]
    public void Ein_abgebrochener_Dialog_laedt_die_Liste_nicht_neu()
    {
        var cut = Zeige(p => p.Add(x => x.VarianteAnlegenOeffnen,
                                   () => Task.FromResult(false)));

        Anlegenknopf(cut).Click();

        cut.WaitForAssertion(() => Assert.False(cut.Instance.Laeuft), TimeSpan.FromSeconds(10));
        Assert.Equal(1, _geladen);
    }

    /// <summary>
    /// Hausregel A‑18: <b>kein Delegat, kein Knopf.</b> Ohne Öffner steht „Variante
    /// anlegen" gar nicht erst da — ein Knopf, der beim Drücken nichts tut, wäre die
    /// schlechtere Auskunft.
    /// </summary>
    [Fact]
    public void Ohne_Oeffner_gibt_es_den_Anlegeknopf_nicht()
    {
        var cut = Zeige();

        Assert.DoesNotContain(Pflegeknoepfe(cut), b => b.TextContent.Contains("Variante anlegen"));
    }

    [Fact]
    public void Loeschen_ist_ohne_markierte_Variante_gesperrt()
    {
        var cut = Zeige();   // Stammzeile markiert

        Assert.True(Loeschknopf(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void Loeschen_fragt_nach_und_loescht_erst_bei_Ja()
    {
        int geloescht = 0;
        bool alle = true;
        var cut = Zeige(p => p
            .Add(x => x.LoeschFrage, () => "Variante „WP klein“ wirklich löschen?")
            .Add(x => x.VarianteLoeschen, (bool a) => { geloescht++; alle = a; return "gelöscht."; }),
            stand: Unterschiedsansicht());

        Loeschknopf(cut).Click();
        Assert.Contains("wirklich löschen", cut.Find(".epos-rueckfrage-text").TextContent);

        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // Nein
        Assert.Equal(0, geloescht);

        Loeschknopf(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        Assert.Equal(1, geloescht);
        Assert.Equal("gelöscht.", cut.Instance.Status);

        // Ohne Mehrdeutigkeit wird das Loeschen ALLER Gleichnamigen NICHT freigegeben.
        Assert.False(alle);
        Assert.False(cut.Instance.MehrdeutigOffen);
    }

    // =====================================================================
    //  Entscheid W15a-O-4 — der Projektname trifft mehrere Projekte
    // =====================================================================

    /// <summary>Der Parametersatz der zweiten Rückfrage (Texte wie im <c>ProjektWahlDialog</c>).</summary>
    private static void MitMehrdeutigkeit(
        Bunit.ComponentParameterCollectionBuilder<UebersichtSeite> p, int anzahl)
    {
        p.Add(x => x.NamensAnzahl, (string _) => anzahl);
        p.Add(x => x.MehrdeutigTitel, "Projektname mehrfach vergeben");
        p.Add(x => x.MehrdeutigFormat,
              "Der Projektname „{0}“ ist {1}-mal vergeben. Alle {1} Projekte werden gelöscht. Fortfahren?");
    }

    /// <summary>
    /// Entscheid O-4 vom 04.09.2026: Trifft der Projektname MEHRERE Projekte, kommt nach
    /// der unveränderten Löschfrage eine zweite Rückfrage — dieselbe wie beim
    /// Projektlöschen (O-3). „Nein" lässt die Seite stehen, gelöscht wird nichts.
    /// </summary>
    [Fact]
    public void Ein_mehrdeutiger_Projektname_fragt_ein_zweites_Mal_und_Nein_loescht_nichts()
    {
        int geloescht = 0;
        var cut = Zeige(p =>
        {
            p.Add(x => x.LoeschFrage, () => "Variante „WP klein“ wirklich löschen?");
            p.Add(x => x.VarianteLoeschen, (bool _) => { geloescht++; return "gelöscht."; });
            MitMehrdeutigkeit(p, 2);
        }, stand: Unterschiedsansicht());

        Loeschknopf(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja auf die Loeschfrage

        // Jetzt steht die ZWEITE Rueckfrage - und geloescht ist noch nichts.
        cut.WaitForAssertion(() => Assert.True(cut.Instance.MehrdeutigOffen),
                          TimeSpan.FromSeconds(10));
        Assert.Equal(0, geloescht);

        string text = cut.Find(".epos-rueckfrage-text").TextContent;
        Assert.Contains("Musterhaus", text);       // {0} - der Projektname der markierten Zeile
        Assert.Contains("2-mal", text);            // {1} - die Anzahl aus NamensAnzahl

        // Vorgabe "Nein": der zweite Knopf traegt die Betonung, nicht der erste.
        IReadOnlyList<IElement> knoepfe = cut.FindAll(".epos-rueckfrage .epos-leiste button");
        Assert.DoesNotContain("epos-knopf--primaer", knoepfe[0].ClassName ?? "");
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassName ?? "");

        knoepfe[1].Click();                        // Nein
        cut.WaitForAssertion(() => Assert.False(cut.Instance.MehrdeutigOffen),
                          TimeSpan.FromSeconds(10));
        Assert.Equal(0, geloescht);
    }

    /// <summary>„Ja" auf die zweite Rückfrage löscht wie bisher — alle Gleichnamigen.</summary>
    [Fact]
    public void Ein_Ja_auf_die_zweite_Rueckfrage_gibt_alle_Gleichnamigen_frei()
    {
        int geloescht = 0;
        bool alle = false;
        var cut = Zeige(p =>
        {
            p.Add(x => x.LoeschFrage, () => "Variante „WP klein“ wirklich löschen?");
            p.Add(x => x.VarianteLoeschen, (bool a) => { geloescht++; alle = a; return "gelöscht."; });
            MitMehrdeutigkeit(p, 2);
        }, stand: Unterschiedsansicht());

        Loeschknopf(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja auf die Loeschfrage
        cut.WaitForAssertion(() => Assert.True(cut.Instance.MehrdeutigOffen),
                          TimeSpan.FromSeconds(10));

        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja auf die Mehrdeutigkeit

        cut.WaitForAssertion(() => Assert.Equal(1, geloescht), TimeSpan.FromSeconds(10));
        Assert.True(alle);
        Assert.False(cut.Instance.MehrdeutigOffen);
        Assert.Equal("gelöscht.", cut.Instance.Status);
    }

    /// <summary>
    /// Ein eindeutiger Name kommt ohne die zweite Rückfrage aus — der Regelfall, denn
    /// <c>Tab_Projekt</c> trägt den eindeutigen Index <c>Projektname</c>.
    /// </summary>
    [Fact]
    public void Ein_eindeutiger_Projektname_loescht_ohne_zweite_Rueckfrage()
    {
        int geloescht = 0;
        var cut = Zeige(p =>
        {
            p.Add(x => x.LoeschFrage, () => "Variante „WP klein“ wirklich löschen?");
            p.Add(x => x.VarianteLoeschen, (bool _) => { geloescht++; return "gelöscht."; });
            MitMehrdeutigkeit(p, 1);
        }, stand: Unterschiedsansicht());

        Loeschknopf(cut).Click();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[0].Click();   // Ja

        cut.WaitForAssertion(() => Assert.Equal(1, geloescht), TimeSpan.FromSeconds(10));
        Assert.False(cut.Instance.MehrdeutigOffen);
    }

    // =====================================================================
    // Simulation
    // =====================================================================

    [Fact]
    public void Die_Simulation_meldet_ihr_Protokoll_im_Fenster()
    {
        var cut = Zeige(p => p.Add(x => x.Simulation, (Action<Laufschritt> m) =>
        {
            m(new Laufschritt(1, 2, "Stammprojekt"));
            return Task.FromResult(new LaufErgebnis
            {
                Erfolg = true,
                Statuszeile = "2 Lauf/Läufe beendet.",
                Meldung = "Stamm „Musterhaus“: 8760 Stunden"
            });
        }));

        Simulierknopf(cut).Click();

        Assert.Equal("2 Lauf/Läufe beendet.", cut.Instance.Status);
        Assert.Contains("8760 Stunden", cut.Find(".epos-warnbanner").TextContent);
        Assert.Equal(2, _geladen);
    }

    // =====================================================================
    // Übernahme
    // =====================================================================

    [Fact]
    public void Der_Uebernahmeknopf_oeffnet_den_Dialog_in_der_Ueberlagerung()
    {
        VergleichZeile? gefragt = null;
        var cut = Zeige(p => p.Add(x => x.UebernahmeGaben, (VergleichZeile z) =>
        {
            gefragt = z;
            return LeererSatz();
        }), stand: Unterschiedsansicht());

        Vergleichszeilen(cut)[0].QuerySelector(".epos-zellenaktionen button")!.Click();

        Assert.NotNull(gefragt);
        Assert.Equal(7, gefragt!.Schluessel);
        Assert.True(cut.Instance.UebernahmeOffen);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Ein_Abbruch_im_Uebernahmedialog_schreibt_nichts()
    {
        int uebernommen = 0;
        var cut = Zeige(p => p
            .Add(x => x.UebernahmeGaben, (VergleichZeile _) => LeererSatz())
            .Add(x => x.Uebernehmen, (VergleichZeile z, int q) => { uebernommen++; return "ok"; }),
            stand: Unterschiedsansicht());

        Vergleichszeilen(cut)[0].QuerySelector(".epos-zellenaktionen button")!.Click();
        cut.Find(".epos-ueberlagerung").KeyDown("Escape");

        Assert.False(cut.Instance.UebernahmeOffen);
        Assert.Equal(0, uebernommen);
    }

    [Fact]
    public void Ohne_Gaben_bleibt_der_Uebernahmeknopf_weg()
    {
        var cut = Zeige(stand: Unterschiedsansicht());

        Assert.Empty(cut.FindAll(".epos-zellenaktionen button"));
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_alten_Maske()
    {
        var cut = Zeige();

        Assert.Equal("UcBkUebersicht.btn_Help", cut.Instance.HilfeSchluessel);
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>
    /// Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c> —
    /// derselbe Weg wie in <c>ListenrahmenTests</c>.
    /// </summary>
    private static string Stilblock(string selektor)
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }

    // =====================================================================
    //  Die Vergleichswahl (Anwenderwunsch 08.09.2026, W5-B-5)
    // =====================================================================

    [Fact]
    public void Ohne_Delegaten_steht_keine_Vergleichswahl()
    {
        var cut = Zeige();
        Assert.Empty(cut.FindAll(".epos-vergleichswahl"));
    }

    [Fact]
    public void Die_Vergleichswahl_nennt_jede_Version_und_meldet_die_Abwahl()
    {
        IReadOnlyList<int>? gemeldet = null;
        var cut = Zeige(p => p.Add(x => x.VergleichGewaehlt, (IReadOnlyList<int> l) => gemeldet = l));

        var haken = cut.FindAll(".epos-vergleichswahl input[type=checkbox]");
        Assert.Equal(2, haken.Count);
        Assert.True(haken[0].HasAttribute("disabled"));      // der Stamm: Referenz, fest
        Assert.True(haken[1].HasAttribute("checked"));
        Assert.Contains("WP klein", cut.Find(".epos-vergleichswahl").TextContent);

        haken[1].Change(false);

        Assert.Equal(new[] { 1030 }, gemeldet);
        Assert.Equal(2, _geladen);                            // die Seite liest den Stand neu
    }
}
