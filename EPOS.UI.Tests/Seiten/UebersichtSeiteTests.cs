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
public class UebersichtSeiteTests : BunitContext
{
    public UebersichtSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        DeutscheOberflaeche();
    }

    /// <summary>
    /// Die Sprache der Oberfläche wird auf de-DE gepinnt (Muster
    /// <c>ProjektListeTests</c>) — Kultur UND Thread-Kultur, damit ein Lauf unter
    /// <c>LANG=en_US.UTF-8</c> dieselben Beschriftungen und dieselbe Zahlschreibweise
    /// sieht.
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    // ---- Probendaten -----------------------------------------------------

    private static VarianteZeile[] Zeilen() => new[]
    {
        new VarianteZeile { IdProjekt = 1030, Art = "Stamm", Bezeichner = "(Stammprojekt)",
                            Projektname = "Musterhaus", SimStand = "02.09.26 10:00",
                            SimZeitpunkt = "02.09.26 10:00", IstStamm = true },
        new VarianteZeile { IdProjekt = 1031, Art = "Variante", Bezeichner = "WP klein",
                            Projektname = "Musterhaus", SimStand = "— (fehlt) ⚠",
                            SimZeitpunkt = "", Auffaellig = true }
    };

    /// <summary>Die Gegenüberstellung (Stammzeile markiert) — ohne Aktionsspalte.</summary>
    private static UebersichtStand Vergleichsansicht() => new UebersichtStand
    {
        Staemme = new[] { (1030, "Musterhaus"), (1040, "Bürohaus") },
        StammId = 1030,
        Zeilen = Zeilen(),
        MarkierteId = 1030,
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
        Assert.Single(cut.FindAll(".epos-variantenzeile input[type=text]"));  // Bezeichner
        Assert.Equal(3, Pflegeknoepfe(cut).Count);                      // Anlegen, Löschen, Simulieren
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
    //  Die Versionswahl (Anwenderwunsch 05.09.2026, W5‑E‑1)
    // =====================================================================

    /// <summary>
    /// Der Stamm steht zuerst, dann die Varianten; der Text ist
    /// „Bezeichner — Projektname" — die zwei Spalten, die die Tabelle bis W5‑E‑1
    /// nebeneinander führte. Die Ids sind die Projekt-Ids (stabil über einen
    /// Neuaufbau, dublettenfrei — die Bedingung der Hausregel für den Wirt eines
    /// <c>Auswahlfeld</c>).
    /// </summary>
    [Fact]
    public void Das_Auswahlfeld_fuehrt_Stamm_und_Varianten_in_der_Reihenfolge_der_Gruppe()
    {
        var cut = Zeige();

        IReadOnlyList<IElement> eintraege = Versionseintraege(cut);
        Assert.Equal(2, eintraege.Count);

        Assert.Equal("1030", eintraege[0].GetAttribute("value"));
        Assert.Equal("(Stammprojekt) — Musterhaus", eintraege[0].TextContent);

        Assert.Equal("1031", eintraege[1].GetAttribute("value"));
        Assert.Equal("WP klein — Musterhaus", eintraege[1].TextContent);
    }

    /// <summary>Gewählt ist, was der Stand als markiert meldet.</summary>
    [Fact]
    public void Das_Auswahlfeld_zeigt_die_markierte_Version()
    {
        Assert.Equal("1030", Versionswahl(Zeige()).GetAttribute("value"));
        Assert.Equal("1031", Versionswahl(Zeige(stand: Unterschiedsansicht())).GetAttribute("value"));
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
        Assert.True(Pflegeknoepfe(cut)[1].HasAttribute("disabled"));      // Stamm: nicht löschbar

        Versionswahl(cut).Change("1031");

        Assert.Equal(1031, cut.Instance.MarkierteId);
        Assert.True(cut.Instance.MitAktionsspalte);
        Assert.Contains("WP klein", cut.Find(".epos-untergruppe").TextContent);
        Assert.False(Pflegeknoepfe(cut)[1].HasAttribute("disabled"));     // Variante: löschbar
        Assert.Contains("noch nicht simuliert", Simulationszeile(cut).TextContent);
    }

    // =====================================================================
    // Variante anlegen und löschen
    // =====================================================================

    [Fact]
    public void Anlegen_gibt_den_Bezeichner_weiter_und_meldet()
    {
        string? bezeichner = null;
        var cut = Zeige(p => p.Add(x => x.VarianteAnlegen, (string b) =>
        {
            bezeichner = b;
            return "Variante „Kessel groß“ wurde angelegt.";
        }));

        cut.Find(".epos-variantenzeile input[type=text]").Input("Kessel groß");
        Pflegeknoepfe(cut)[0].Click();

        Assert.Equal("Kessel groß", bezeichner);
        Assert.Contains("wurde angelegt", cut.Instance.Status);
        Assert.Equal(2, _geladen);
    }

    [Fact]
    public void Loeschen_ist_ohne_markierte_Variante_gesperrt()
    {
        var cut = Zeige();   // Stammzeile markiert

        Assert.True(Pflegeknoepfe(cut)[1].HasAttribute("disabled"));
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

        Pflegeknoepfe(cut)[1].Click();
        Assert.Contains("wirklich löschen", cut.Find(".epos-rueckfrage-text").TextContent);

        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();   // Nein
        Assert.Equal(0, geloescht);

        Pflegeknoepfe(cut)[1].Click();
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

        Pflegeknoepfe(cut)[1].Click();
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

        Pflegeknoepfe(cut)[1].Click();
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

        Pflegeknoepfe(cut)[1].Click();
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

        Pflegeknoepfe(cut)[2].Click();

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
}
