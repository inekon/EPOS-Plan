using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Strom;
using EPOS.UI.Standards;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Seiten.Strom;

/// <summary>
/// STATION 4 „OPTIMIERUNG" (Auftrag #224, Anwenderentscheid SD‑E‑9 „Empfehlung" vom
/// 11.09.2026, Konzept „Stromspeicher-Dialoge" 7.4).
///
/// <para><b>Der Befund, den sie beantwortet</b> (Konzept 7.1/7.2): Die Rastersuche gab
/// es die ganze Zeit — erreichbar nur über DREI Schalter an DREI Orten, und das Wort
/// „Optimierung" kam in der Ablaufleiste nicht vor. Station 4 hieß „Bewerten".</para>
///
/// <para><b>Was hier geprüft wird:</b> der Aufbau der Station nach dem Mappenblatt V7 —
/// Kopf mit Ziel und Suchwahl, Suchraum je Einheit als Tabelle, die Kandidatenzeile
/// LIVE gegen die Grenze, der Feinraster-Schalter, der Rechenknopf und der Kasten
/// „Bestes Ergebnis". Und die zwei Umzüge: Schritt 1 ohne Größenbereich, Schritt 2 mit
/// der Jahresprojektion.</para>
/// </summary>
public sealed class OptimierungStationTests : EposBunitContext
{
    /// <summary>Die Ansicht trägt einen <c>InfoKnopf</c> und braucht deshalb den Hilfedienst.</summary>
    public OptimierungStationTests()
        => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    // =====================================================================
    //  Der Kopf: Ziel und die Wahl „bewerten" / „suchen"
    // =====================================================================

    /// <summary>
    /// Der Kopf nennt das ZIEL im Klartext — der Kapitalwert gegenüber „ohne Speicher"
    /// (SD‑Q11; die Mappe V7 maximiert den Jahresüberschuss, das Programm den
    /// Kapitalwert) — und stellt die zwei Wege zur Wahl.
    /// </summary>
    [Fact]
    public void Der_Kopf_nennt_das_Ziel_und_die_zwei_Wege()
    {
        var cut = Station();

        Assert.Contains(Resource.FLOTTE_OPT_ZIEL_TEXT, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_SUCHE_AUS, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_METHODE_GROESSE, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_METHODE_STUECKZAHL, cut.Markup, StringComparison.Ordinal);

        // Drei Optionen, je ein Erklaersatz (Konzept 8.3, SD-Q13).
        Assert.Equal(3, cut.FindAll(".epos-flotte-suche input[type=radio]").Count);
        Assert.Equal(3, cut.FindAll(".epos-flotte-suche p.epos-option-beschreibung").Count);
    }

    /// <summary>
    /// Die Wahl SETZT beide Felder desselben Standes — die <c>Suchmethode</c> der Engine
    /// und <c>FlottenGroessenOptimieren</c>, an dem der Kern entscheidet, ob überhaupt
    /// eine Rastersuche läuft — und der Suchraum erscheint erst dann.
    /// </summary>
    [Fact]
    public void Die_Wahl_schaltet_die_Suchmethode_und_zeigt_erst_dann_den_Suchraum()
    {
        var cut = Station(FlottenSuchmethode.Bewerten);

        Assert.DoesNotContain(Resource.FLOTTE_OPT_SUCHRAUM, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_BEWERTEN_HINWEIS, cut.Markup, StringComparison.Ordinal);

        Suchwahl(cut, Resource.FLOTTE_OPT_METHODE_GROESSE).Change(true);

        Assert.True(cut.Instance.Eingaben.Auslegung!.FlottenGroessenOptimieren);
        Assert.Equal(FlottenSuchmethode.Groesse,
                     cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Suchmethode);
        Assert.Contains(Resource.FLOTTE_OPT_SUCHRAUM, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Die zweite Methode</b> (Auftrag #247, SD‑Q14/SD‑Q16): „Stückzahl suchen"
    /// variiert an jeder eingeschalteten Einheit die Stückzahl — der Rumpf ihrer Karte
    /// zeigt dann die zwei Stückzahlfelder statt der Bereiche, der Quellenwahl und der
    /// Geräteliste. <b>Sie bleibt unverändert</b>: Dort ist das Gerät fest und die
    /// Stückzahl die Variable; die beiden Methoden ergeben das Paar „welches Gerät" und
    /// „wie viele davon".
    /// </summary>
    [Fact]
    public void Die_Stueckzahlsuche_tauscht_den_Rumpf_der_Karte()
    {
        var cut = Station(FlottenSuchmethode.Groesse);

        Assert.Contains(Resource.FLOTTE_OPT_QUELLE, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_GERAETE_TITEL, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_KAPAZITAET_VON, cut.Markup, StringComparison.Ordinal);

        Suchwahl(cut, Resource.FLOTTE_OPT_METHODE_STUECKZAHL).Change(true);

        Assert.Equal(FlottenSuchmethode.Stueckzahl,
                     cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Suchmethode);
        Assert.True(cut.Instance.Eingaben.Auslegung!.FlottenGroessenOptimieren);
        Assert.DoesNotContain(Resource.FLOTTE_OPT_GERAETE_TITEL, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Resource.FLOTTE_ED_KAPAZITAET_VON, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_ANZAHL_VON, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Die Kandidatenzeile hängt an der Methode</b> — dieselbe Zählregel des Kerns,
    /// anderes Ergebnis: unter „Größe" das Größenraster bei fester Stückzahl, unter
    /// „Stückzahl" die Stückzahlen bei fester Größe.
    /// </summary>
    [Fact]
    public void Die_Kandidatenzeile_zaehlt_je_Methode_anders()
    {
        var cut = Station(FlottenSuchmethode.Groesse);

        // VIER Geraete des Bestands fallen in beide Bereiche.
        Assert.Contains("4", cut.Find("p.epos-flotte-kandidatenzeile").TextContent,
                        StringComparison.Ordinal);

        Suchwahl(cut, Resource.FLOTTE_OPT_METHODE_STUECKZAHL).Change(true);

        // Stueckzahl 1…3 (siehe Pruefstand) = 3 Kandidaten.
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(
            cut.Instance.Eingaben.Auslegung!.Flotte!);
        Assert.Equal(3, zahl.Grob);
        Assert.Equal(0, zahl.FeinHoechstens);
        Assert.Contains("3", cut.Find("p.epos-flotte-kandidatenzeile").TextContent,
                        StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Ohne eine Einheit mit „variieren" ist die STÜCKZAHLSUCHE nicht wählbar</b>
    /// (Konzept 8.3): Sie steht gedimmt da und die ABHILFE sagt, was zu tun ist.
    /// Gedimmt heißt WEICH gesperrt — ein <c>disabled</c>-Bedienelement könnte seinen
    /// Grund gar nicht zeigen (Hausregel W16b‑E‑6).
    ///
    /// <para><b>Die Größensuche bleibt wählbar</b> (Auftrag #273): Sie variiert seither
    /// JEDE Einheit und braucht deshalb nur eine — den Schalter „variieren" gibt es
    /// unter ihr gar nicht mehr.</para>
    /// </summary>
    [Fact]
    public void Ohne_variierte_Einheit_steht_die_Stueckzahlsuche_gedimmt_mit_Abhilfe()
    {
        var cut = Station(FlottenSuchmethode.Stueckzahl, variieren: false);

        IElement groesse = Suchwahl(cut, Resource.FLOTTE_OPT_METHODE_GROESSE);
        IElement stueck = Suchwahl(cut, Resource.FLOTTE_OPT_METHODE_STUECKZAHL);
        IElement bewerten = Suchwahl(cut, Resource.FLOTTE_OPT_SUCHE_AUS);

        Assert.Equal("true", stueck.GetAttribute("aria-disabled"));
        Assert.False(stueck.HasAttribute("disabled"));       // WEICH, damit der Grund ankommt
        Assert.NotEqual("true", groesse.GetAttribute("aria-disabled"));
        Assert.NotEqual("true", bewerten.GetAttribute("aria-disabled"));

        Assert.Single(cut.FindAll("p.epos-flotte-abhilfe"));
        Assert.Contains(Resource.FLOTTE_OPT_ABHILFE, cut.Markup, StringComparison.Ordinal);

        // Und der Rechenknopf ist gesperrt — der Lauf wuerde im Kern benannt abgelehnt.
        Assert.True(cut.Find(".epos-flotte-optimierung-lauf button").HasAttribute("disabled"));
    }

    /// <summary>
    /// <b>Die Station ist seit Auftrag #273 einspaltig</b> (Anwenderentscheid vom
    /// 14.09.2026): erst die Suche, darunter der Bedienblock in voller Breite, darunter
    /// der Suchraum. Bis dahin saß der Bedienblock als Kasten fester Breite RECHTS
    /// neben den Optionen.
    /// </summary>
    [Fact]
    public void Der_Bedienblock_steht_unter_der_Suche_und_vor_dem_Suchraum()
    {
        var cut = Station();

        IElement kopf = cut.Find(".epos-flotte-optimierung-kopf");
        IElement[] kinder = kopf.Children.ToArray();

        Assert.Equal(2, kinder.Length);
        Assert.Contains("epos-flotte-suche", kinder[0].ClassName!, StringComparison.Ordinal);
        Assert.Contains("epos-flotte-bedienblock", kinder[1].ClassName!, StringComparison.Ordinal);

        // Und der Suchraum folgt DANACH, nicht daneben.
        string[] klassen = cut.Find(".epos-flotte-optimierung").Children
                              .Select(x => x.ClassName ?? "").ToArray();
        int kopfstelle = Array.FindIndex(klassen,
            x => x.Contains("epos-flotte-optimierung-kopf", StringComparison.Ordinal));
        int raumstelle = Array.FindIndex(klassen,
            x => x.Contains("epos-flotte-abschnitt", StringComparison.Ordinal));
        Assert.True(kopfstelle >= 0 && raumstelle > kopfstelle,
                    "Der Suchraum steht nicht hinter dem Kopfblock.");
    }

    /// <summary>
    /// <b>Station 4 zeigt seit #273 KEIN Ergebnis mehr</b> (Konzept 7.10): weder den
    /// Kasten „Bestes Ergebnis" noch die Größen-Sicht — beide stehen in Station 5 unter
    /// der Rubrik „Ergebnisse der Optimierung".
    /// </summary>
    [Fact]
    public void Station_vier_traegt_nach_dem_Lauf_kein_Ergebnis()
    {
        var cut = Gerechnet();

        // Nach dem Lauf steht Station 5 vorn, und dort sind beide Rubriken.
        Assert.Equal(AuslegungSchritt.Ergebnis, cut.Instance.Schritt);
        Assert.Single(cut.FindAll(".epos-flotte-bestes"));
        Assert.Contains(Resource.FLOTTE_ERG_OPTIMIERUNG, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ERG_SIMULATION, cut.Markup, StringComparison.Ordinal);

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);

        Assert.Empty(cut.FindAll(".epos-flotte-bestes"));
        Assert.Empty(cut.FindComponents<SpeicherFlottenGroessenAnsicht>());
        Assert.Empty(cut.FindComponents<OptimierungsergebnisBlock>());
    }

    // =====================================================================
    //  Der Suchraum als Tabelle
    // =====================================================================

    /// <summary>
    /// EINE KARTE JE EINHEIT (Konzept 8.4, SD‑Q18) — unter <b>„Stückzahl suchen"</b> mit
    /// Kopfzeile aus Namen, festen Kenndaten und dem Schalter „variieren", Rumpf je
    /// Methode, Fußzeile mit den Kandidaten DIESER Einheit. Die Neun-Spalten-Tabelle
    /// gibt es nicht mehr.
    /// </summary>
    [Fact]
    public void Der_Suchraum_traegt_unter_Stueckzahl_eine_Karte_je_Einheit()
    {
        var cut = Station(FlottenSuchmethode.Stueckzahl);

        Assert.Empty(cut.FindAll("table.epos-flotte-suchraum"));

        IElement karte = Assert.Single(cut.FindAll("article.epos-flotte-einheitskarte"));
        Assert.Contains("Hauptspeicher",
                        karte.QuerySelector(".epos-flotte-einheitskarte__name")!.TextContent,
                        StringComparison.Ordinal);
        Assert.Contains("100", karte.QuerySelector(".epos-flotte-einheitskarte__kenndaten")!.TextContent,
                        StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_SP_VARIIEREN, karte.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_KARTE_KANDIDATEN,
                        karte.QuerySelector(".epos-flotte-einheitskarte__fuss")!.TextContent,
                        StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Unter „Größe suchen" nennt der Suchraum kein Gerät</b> (Auftrag #273,
    /// Anwenderrückmeldung vom 14.09.2026): keine Kopfzeile mit Hersteller und Typ,
    /// keine festen Kenndaten, kein Schalter „variieren" — die Suche variiert Kapazität,
    /// Leistung und C-Rate, und welches Produkt dahintersteht, beantwortet keine der
    /// gestellten Fragen. Der Suchraum selbst bleibt vollständig.
    /// </summary>
    [Fact]
    public void Unter_Groesse_nennt_die_Karte_kein_Geraet()
    {
        var cut = Station(FlottenSuchmethode.Groesse);

        IElement karte = Assert.Single(cut.FindAll("article.epos-flotte-einheitskarte"));

        Assert.DoesNotContain("Hauptspeicher", karte.TextContent, StringComparison.Ordinal);
        Assert.Empty(karte.QuerySelectorAll(".epos-flotte-einheitskarte__kopf"));
        Assert.Empty(karte.QuerySelectorAll(".epos-flotte-einheitskarte__kenndaten"));
        Assert.DoesNotContain(Resource.FLOTTE_OPT_SP_VARIIEREN, karte.TextContent,
                              StringComparison.Ordinal);

        // Der Suchraum steht vollstaendig — samt Kandidatenzahl dieser Einheit.
        Assert.Contains(Resource.FLOTTE_OPT_QUELLE, karte.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_KAPAZITAET_VON, karte.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_LEISTUNG_BIS, karte.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_KARTE_KANDIDATEN,
                        karte.QuerySelector(".epos-flotte-einheitskarte__fuss")!.TextContent,
                        StringComparison.Ordinal);

        // Und der Satz, der die fehlende Produktzeile erklaert.
        Assert.Contains(Resource.FLOTTE_OPT_GROESSE_UNABHAENGIG, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Mehrere Einheiten heißen unter „Größe suchen" neutral</b> (Auftrag #273):
    /// „Einheit 1", „Einheit 2" — ohne Produktnamen, aber unterscheidbar. Und eine
    /// abgeschaltete Achse gibt es dort nicht mehr: Die Station variiert jede Einheit
    /// und stellt den Stand entsprechend.
    /// </summary>
    [Fact]
    public void Unter_Groesse_heissen_zwei_Einheiten_neutral_und_werden_beide_variiert()
    {
        var cut = Station(FlottenSuchmethode.Groesse, zweiteEinheit: true, variieren: false);

        IReadOnlyList<IElement> karten = cut.FindAll("article.epos-flotte-einheitskarte");
        Assert.Equal(2, karten.Count);
        Assert.Contains(string.Format(Kultur, Resource.FLOTTE_GROESSEN_EINHEIT, 1),
                        karten[0].TextContent, StringComparison.Ordinal);
        Assert.Contains(string.Format(Kultur, Resource.FLOTTE_GROESSEN_EINHEIT, 2),
                        karten[1].TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Hauptspeicher", cut.Find("section.epos-flotte-abschnitt").TextContent,
                              StringComparison.Ordinal);

        Assert.All(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen,
                   a => Assert.True(a.Aktiv));
        Assert.Empty(cut.FindAll("article.epos-flotte-einheitskarte p.epos-flotte-fest"));
    }

    /// <summary>
    /// Eine Einheit OHNE „variieren" verschwindet nicht — ihre Karte steht GEDIMMT da
    /// und sagt mit „fest: …", mit welchem Wert sie in jeden Kandidaten eingeht. Den
    /// Schalter gibt es seit #273 nur noch unter „Stückzahl suchen".
    /// </summary>
    [Fact]
    public void Eine_Einheit_ohne_variieren_steht_gedimmt_mit_ihrem_festen_Wert()
    {
        var cut = Station(FlottenSuchmethode.Stueckzahl, variieren: false);

        IElement karte = Assert.Single(cut.FindAll("article.epos-flotte-einheitskarte"));
        Assert.Contains("epos-flotte-einheitskarte--gedimmt", karte.ClassName!, StringComparison.Ordinal);
        Assert.Single(karte.QuerySelectorAll("p.epos-flotte-fest"));
        Assert.Empty(karte.QuerySelectorAll(".epos-flotte-einheitskarte__rumpf"));
        Assert.Contains(Resource.FLOTTE_OPT_KARTE_FEST, karte.TextContent, StringComparison.Ordinal);
    }

    /// <summary>Ein Bereich der Karte schreibt in die Achse der Einheit.</summary>
    [Fact]
    public void Ein_Bereich_der_Karte_schreibt_in_die_Achse()
    {
        var cut = Station();

        Kartenfeld(cut, Resource.FLOTTE_ED_KAPAZITAET_BIS).Input("640");

        FlottenAuslegungsAchse achse =
            Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen);
        Assert.Equal(640.0, achse.KapazitaetBisKWh, 9);
    }

    /// <summary>
    /// Der Schalter „variieren" auf der Karte SETZT <c>FlottenAuslegungsAchse.Aktiv</c> —
    /// er entscheidet unter „Stückzahl suchen", WELCHE Einheiten die Methode variiert
    /// (Konzept 8.3, SD‑Q15). Unter „Größe suchen" gibt es ihn seit #273 nicht mehr.
    /// </summary>
    [Fact]
    public void Der_Schalter_variieren_setzt_die_Achse_der_Einheit()
    {
        var cut = Station(FlottenSuchmethode.Stueckzahl);

        IElement schalter = cut.Find("article.epos-flotte-einheitskarte .epos-schalter input");
        schalter.Change(false);

        Assert.False(Assert.Single(
            cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen).Aktiv);
    }

    // =====================================================================
    //  #245 — der Fokus bleibt beim Tippen im Feld
    // =====================================================================

    /// <summary>
    /// EINE EINGABE LÄSST DIE ZEILE STEHEN (Anwenderbefund <b>#245</b>, 12.09.2026:
    /// „bei jeder Tastatureingabe springt der Fokus aus dem Feld").
    /// </summary>
    /// <remarks>
    /// <para><b>Was hier wirklich gemessen wird.</b> Nicht die DOM-Knoten — bunit liest
    /// das Markup nach jedem Zeichenlauf, der es ÄNDERT, komplett neu ein; ein
    /// <c>&lt;tr&gt;</c> ist danach IMMER eine andere AngleSharp-Instanz, auch wenn
    /// Blazor die Zeile im Browser stehen ließe. Gemessen wird deshalb die Identität der
    /// KOMPONENTEN in der Zeile: Bleibt jedes <c>Zahlenfeld</c>/<c>Ganzzahlfeld</c>
    /// dieselbe Instanz, hat Blazor den Teilbaum behalten — und damit im Browser auch
    /// dessen <c>&lt;input&gt;</c> samt Fokus. Wird er abgerissen, sind es neue
    /// Instanzen.</para>
    /// <para><b>Vor dem Fix war das so</b>: <c>&lt;tr @@key="a"&gt;</c> hing an der
    /// <c>FlottenAuslegungsAchse</c> (Referenzvergleich), und
    /// <c>StromspeicherAuslegungSeite.FlotteGeschrieben</c> ersetzt die Konfiguration
    /// nach JEDEM gemeldeten Wert durch eine JSON-Tiefenkopie — neuer Schlüssel je
    /// Tastendruck, Zeile weg. Seither steht dort eine Wertidentität
    /// (<c>OptimierungBlock.Zeilenschluessel</c>).</para>
    /// </remarks>
    [Fact]
    public void Eine_Eingabe_im_Suchraum_laesst_die_Zeile_und_ihre_Felder_stehen()
    {
        var cut = Station();

        IReadOnlyList<object> vorher = Zeilenkomponenten(cut);
        // Die Karte fuehrt unter „Groesse suchen" VIER Zahlenfelder: Kapazitaet und
        // Leistung, je von/bis. Schrittweiten gibt es nicht mehr (gesucht wird unter
        // vorhandenen Geraeten), die C-Rate ist abgeleitet, und die Stueckzahl steht fest.
        Assert.Equal(4, vorher.Count);

        Kartenfeld(cut, Resource.FLOTTE_ED_KAPAZITAET_BIS).Input("640");

        IReadOnlyList<object> nachher = Zeilenkomponenten(cut);
        Assert.Equal(vorher.Count, nachher.Count);
        for (int i = 0; i < vorher.Count; i++) Assert.Same(vorher[i], nachher[i]);

        FlottenAuslegungsAchse achse =
            Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen);
        Assert.Equal(640.0, achse.KapazitaetBisKWh, 9);
    }

    /// <summary>
    /// Die ANGEFANGENE Dezimalzahl bleibt stehen: Wer „640," getippt hat, findet „640,"
    /// vor und nicht „640" — die zweite Hälfte desselben Befunds #245.
    /// </summary>
    /// <remarks>
    /// „640," ist bereits eine gültige Zahl (<c>Zahlen.ZahlParsen</c> nimmt Komma wie
    /// Punkt), der Wert 640 geht also hinaus; <c>Zahlenfeld.OnParametersSet</c> lässt den
    /// Text dann in Ruhe, weil er denselben Wert meint. Riss die Zeile ab, entstand ein
    /// FRISCHES Feld ohne Texterinnerung — es schrieb den Anzeigetext „640" und nahm dem
    /// Anwender mitten in der Eingabe das Trennzeichen weg.
    /// </remarks>
    [Fact]
    public void Eine_angefangene_Dezimalzahl_bleibt_im_Feld_stehen()
    {
        var cut = Station();

        Kartenfeld(cut, Resource.FLOTTE_ED_KAPAZITAET_BIS).Input("640,");

        Assert.Equal("640,", Kartenfeld(cut, Resource.FLOTTE_ED_KAPAZITAET_BIS).GetAttribute("value"));
    }

    /// <summary>
    /// <b>Die Karte kennt nur noch ZWEI Bereiche und die QUELLE</b>: Kapazität und
    /// Leistung. Die C-Rate ist keine Suchachse mehr — sie steht als abgeleitete Kennzahl
    /// in der Geräteliste, aber in keinem Eingabefeld.
    /// </summary>
    [Fact]
    public void Die_Karte_kennt_nur_Kapazitaet_Leistung_und_die_Quelle()
    {
        var cut = Station();

        Assert.Contains(Resource.FLOTTE_ED_KAPAZITAET_VON, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_LEISTUNG_VON, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_QUELLE, cut.Markup, StringComparison.Ordinal);

        Assert.DoesNotContain(Resource.FLOTTE_ED_CRATE_VON, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Resource.FLOTTE_ED_KAPAZITAET_SCHRITT, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Resource.FLOTTE_ED_LEISTUNG_SCHRITT, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Die QUELLE ist wählbar</b>: Projektkatalog oder Stammdaten. Die Wahl steht im
    /// Suchraum und wird mitgespeichert.
    /// </summary>
    [Fact]
    public void Die_Quellenwahl_steht_auf_der_Karte_und_kommt_im_Modell_an()
    {
        var cut = Station();

        FlottenAuslegungsAchse achse =
            Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen);
        Assert.Equal(FlottenKandidatenquelle.Projektkatalog, achse.Quelle);

        cut.Find("article.epos-flotte-einheitskarte select")
           .Change(((int)FlottenKandidatenquelle.Stammdaten).ToString(System.Globalization.CultureInfo.InvariantCulture));

        achse = Assert.Single(cut.Instance.Eingaben.Auslegung!.Flotte!.Auslegung.Achsen);
        Assert.Equal(FlottenKandidatenquelle.Stammdaten, achse.Quelle);
    }

    /// <summary>
    /// <b>Die gefundenen Geräte stehen mit ihrer ABWEICHUNG da.</b> Vier Treffer, jeder
    /// mit 0 % — der Anwender sieht, dass er bekommt, wonach er gefragt hat.
    /// </summary>
    [Fact]
    public void Die_Geraeteliste_nennt_jedes_Geraet_mit_seiner_Abweichung()
    {
        var cut = Station();

        IElement tabelle = cut.Find("table.epos-flotte-geraete");
        Assert.Equal(4, tabelle.QuerySelectorAll("tbody tr").Length);
        Assert.Contains("Speicher 100", tabelle.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Speicher 400", tabelle.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_GERAETE_TREFFER, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Trifft kein Gerät, kommen die nächstliegenden</b> — und die Seite sagt es,
    /// statt sie für Treffer ausgeben zu lassen.
    /// </summary>
    [Fact]
    public void Ohne_Treffer_nennt_die_Seite_die_naechstliegenden()
    {
        var cut = Station();

        Kartenfeld(cut, Resource.FLOTTE_ED_KAPAZITAET_VON).Input("900");
        Kartenfeld(cut, Resource.FLOTTE_ED_KAPAZITAET_BIS).Input("1000");

        cut.WaitForAssertion(() => Assert.Contains(
            string.Format(Kultur, Resource.FLOTTE_OPT_GERAETE_NAEHE, 5),
            cut.Markup, StringComparison.Ordinal));
    }

    // =====================================================================
    //  Die Kandidatenzeile — live, und sie sperrt
    // =====================================================================

    /// <summary>
    /// Die Kandidatenzeile nennt die Zahl der Geräte und die Grenze — und sie kommt aus
    /// der ZÄHLREGEL des Kerns, nicht aus einer zweiten Formel in der Seite. <b>Eine
    /// zweite Phase gibt es nicht mehr</b>: Zwischen zwei Geräten liegt kein drittes.
    /// </summary>
    [Fact]
    public void Die_Kandidatenzeile_nennt_die_Geraetezahl_und_die_Grenze()
    {
        var cut = Station();

        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(
            cut.Instance.Eingaben.Auslegung!.Flotte!);
        IElement zeile = cut.Find("p.epos-flotte-kandidatenzeile");

        Assert.Equal(4, zahl.Grob);
        Assert.Equal(0, zahl.FeinHoechstens);
        Assert.Contains(zahl.Grob.ToString(Kultur), zeile.TextContent, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_OK, zeile.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("epos-flotte-kandidatenzeile--rot", zeile.ClassName!, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Grenzfall.</b> Reißt das Raster die Grenze, wird die Zeile ROT, sagt es in
    /// WORTEN (Farbe allein trägt keine Aussage) — und der Rechenknopf ist gesperrt und
    /// nennt den Grund. Bis #224 flog das erst BEIM START als Ausnahme auf.
    /// </summary>
    [Fact]
    public void Ein_zu_grosses_Raster_faerbt_die_Zeile_rot_und_sperrt_den_Rechenknopf()
    {
        var cut = Station(maximaleKandidaten: 3);

        IElement zeile = cut.Find("p.epos-flotte-kandidatenzeile");
        Assert.Contains("epos-flotte-kandidatenzeile--rot", zeile.ClassName!, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_ZUVIEL, zeile.TextContent, StringComparison.Ordinal);

        IElement knopf = cut.Find(".epos-flotte-optimierung-lauf button");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Contains(Resource.FLOTTE_OPT_RASTER_ZUVIEL, knopf.GetAttribute("title")!,
                        StringComparison.Ordinal);
    }

    /// <summary>Die Gegenprobe: Mit weiter Grenze ist derselbe Suchraum zulässig.</summary>
    [Fact]
    public void Mit_weiter_Grenze_ist_derselbe_Suchraum_zulaessig()
    {
        var cut = Station(maximaleKandidaten: 10000);

        Assert.DoesNotContain("epos-flotte-kandidatenzeile--rot",
                              cut.Find("p.epos-flotte-kandidatenzeile").ClassName!,
                              StringComparison.Ordinal);
        Assert.False(cut.Find(".epos-flotte-optimierung-lauf button").HasAttribute("disabled"));
    }

    // =====================================================================
    //  Es gibt keinen Feinraster-Schalter mehr
    // =====================================================================

    /// <summary>
    /// <b>Die Station zeigt keinen Feinraster-Schalter.</b> Die Größensuche wählt unter
    /// vorhandenen Geräten; zwischen zwei Geräten liegt kein drittes, und eine
    /// zwischengerechnete Größe wäre ein Speicher, den es nicht gibt. Der gespeicherte
    /// Schalter bleibt lesbar und wirkungslos.
    /// </summary>
    [Fact]
    public void Es_gibt_keinen_Feinraster_Schalter_mehr()
    {
        var cut = Station();

        Assert.DoesNotContain(Resource.FLOTTE_OPT_FEINRASTER, cut.Markup, StringComparison.Ordinal);
        Assert.Equal(0, FlottenOptimierer.Kandidatenzahl(
            cut.Instance.Eingaben.Auslegung!.Flotte!).FeinHoechstens);
    }

    // =====================================================================
    //  Der Kasten „Bestes Ergebnis" — seit #273 in Station 5
    // =====================================================================

    /// <summary>
    /// Ohne Lauf gibt es in Station 4 keinen Kasten — und Station 5 ist noch gar nicht
    /// betretbar.
    /// </summary>
    [Fact]
    public void Ohne_Suche_steht_kein_Kasten()
    {
        var cut = Station();

        Assert.Empty(cut.FindAll(".epos-flotte-bestes"));
        Assert.DoesNotContain(Resource.FLOTTE_ERG_OPTIMIERUNG, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Nach einer Suche steht der Kasten mit Kapitalwert, jährlicher Ersparnis (SD‑Q11),
    /// Größe, Einheitenzahl, geprüften und zulässigen Kandidaten, Rechendauer — und der
    /// MARKE, aus welcher Phase der Beste stammt. <b>Er steht seit #273 in Station 5</b>,
    /// unter der Rubrik „Ergebnisse der Optimierung".
    /// </summary>
    [Fact]
    public void Nach_einer_Suche_steht_der_Kasten_mit_allen_sieben_Angaben()
    {
        var cut = Gerechnet();

        Assert.Equal(AuslegungSchritt.Ergebnis, cut.Instance.Schritt);
        IElement kasten = cut.Find(".epos-flotte-bestes");

        Assert.Contains("1.500", kasten.TextContent, StringComparison.Ordinal);   // Kapitalwert
        Assert.Contains("180", kasten.TextContent, StringComparison.Ordinal);     // Ersparnis
        Assert.Contains("30", kasten.TextContent, StringComparison.Ordinal);      // Kapazitaet
        Assert.Contains("3", kasten.TextContent, StringComparison.Ordinal);       // geprueft
        Assert.Contains(Resource.FLOTTE_OPT_BEST_GEPRUEFT, kasten.TextContent, StringComparison.Ordinal);

        // Die Phasenmarke sagt: der Beste kommt aus dem FEINRASTER.
        Assert.Contains(Resource.FLOTTE_OPT_MARKE_FEIN,
                        cut.Find(".epos-flotte-phasenmarke").TextContent, StringComparison.Ordinal);
    }

    /// <summary>Ein Bester aus dem Grobraster trägt die andere Marke — die Gegenprobe.</summary>
    [Fact]
    public void Ein_Bester_aus_dem_Grobraster_traegt_die_andere_Marke()
    {
        var cut = Gerechnet(phase: FlottenKandidatPhase.Grob);

        Assert.Contains(Resource.FLOTTE_OPT_MARKE_GROB,
                        cut.Find(".epos-flotte-phasenmarke").TextContent, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Die zwei Umzuege — Schritt 1 und Schritt 2
    // =====================================================================

    /// <summary>
    /// SCHRITT 1 trägt nur noch die Einheiten (Zielbild 7.4): kein Größenbereich, keine
    /// Jahresprojektion, kein Schalter „Speicheranzahl und Größenbereiche optimieren".
    /// </summary>
    [Fact]
    public void Schritt_eins_traegt_nur_noch_die_Einheiten()
    {
        var cut = Station();
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Speicher);

        Assert.Single(cut.FindComponents<SpeicherFlottenEditor>());
        Assert.Empty(cut.FindComponents<OptimierungBlock>());
        Assert.Empty(cut.FindComponents<SpeicherFlottenWirtschaftBlock>());
        Assert.DoesNotContain(Resource.FLOTTE_DLG_CHK_OPTIMIEREN, cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Maximale Auslegungskandidaten", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// SCHRITT 2 trägt die wirtschaftliche Jahresprojektion (SD‑Q12) samt den DREI
    /// Erklärzeilen aus Konzept 7.3 — Ausgleichswert, Restwert der Studie, maximale
    /// Kandidaten.
    /// </summary>
    [Fact]
    public void Schritt_zwei_traegt_die_Jahresprojektion_mit_drei_Erklaerzeilen()
    {
        var cut = Station();
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);

        Assert.Single(cut.FindComponents<SpeicherFlottenWirtschaftBlock>());
        Assert.Contains(Resource.FLOTTE_ED_AUSGLEICH_ERL, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_RESTWERT_ERL, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Resource.FLOTTE_ED_KANDIDATEN_ERL, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// SCHRITT 3 trägt „Netz und Planung" — es beschreibt den BETRIEB und nicht eine
    /// Einheit (Zielbild 7.4).
    /// </summary>
    [Fact]
    public void Schritt_drei_traegt_Netz_und_Planung()
    {
        var cut = Station();
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Betrieb);

        Assert.Single(cut.FindComponents<SpeicherFlottenNetzBlock>());
        Assert.Contains(Resource.FLOTTE_ED_BEZUGSGRENZE, cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Der Ausgleichswert zeigt vier Nachkommastellen</b>, der GESPEICHERTE Wert
    /// bleibt ganz (Konzept 7.8): Der Vorschlag entsteht als mittlerer Bezugspreis und
    /// trägt deshalb einen Gleitkommarest.
    /// </summary>
    [Fact]
    public void Der_Ausgleichswert_wird_gerundet_angezeigt_aber_nicht_gespeichert()
    {
        var cut = Station(ausgleich: 0.31746000000002055);
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);

        IElement feld = cut.FindAll("label")
            .Single(x => x.TextContent.Contains(Resource.FLOTTE_ED_AUSGLEICH, StringComparison.Ordinal))
            .QuerySelector("input")!;

        Assert.Equal("0,3175", feld.GetAttribute("value"));
        Assert.Equal(0.31746000000002055,
                     cut.Instance.Eingaben.Auslegung!.Flotte!.Optionen.EnergieAusgleichEuroProKWh!.Value,
                     15);
    }

    // =====================================================================
    //  Die Vorpruefungshinweise sind kompakt (Konzept 7.8)
    // =====================================================================

    /// <summary>
    /// EIN Hinweis ist EINE Zeile — kein Warnbanner über die ganze Breite. Die
    /// Vorprüfung SPERRT nicht, und eine Warnfläche meldete einen Fehler, den es nicht
    /// gibt.
    /// </summary>
    [Fact]
    public void Ein_Vorpruefungshinweis_ist_eine_Zeile()
    {
        var cut = Station(hinweise: new[] { Hinweis("Betriebsaufwand 1,00 €/a") });

        Assert.Single(cut.FindAll("p.epos-flotte-hinweiszeile"));
        Assert.Empty(cut.FindAll(".epos-flotte-hinweiszeilen--block"));
        Assert.Contains("Betriebsaufwand", cut.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// ZWEI Hinweise stehen in einem aufklappbaren Block — beim ersten Erscheinen
    /// offen, danach entscheidet der Anwender.
    /// </summary>
    [Fact]
    public void Zwei_Vorpruefungshinweise_stehen_in_einem_aufklappbaren_Block()
    {
        var cut = Station(hinweise: new[]
        {
            Hinweis("Betriebsaufwand 1,00 €/a"), Hinweis("Start-Ladezustand 50 %")
        });

        Assert.Single(cut.FindAll(".epos-flotte-hinweiszeilen--block"));
        Assert.Equal(2, cut.FindAll("p.epos-flotte-hinweiszeile").Count);

        cut.Find(".epos-flotte-hinweiszeilen__kopf").Click();

        Assert.Empty(cut.FindAll("p.epos-flotte-hinweiszeile"));
        Assert.Single(cut.FindAll(".epos-flotte-hinweiszeilen--block"));
    }

    // ================================================================= Prüfstand

    private static System.Globalization.CultureInfo Kultur
        => System.Globalization.CultureInfo.CurrentCulture;

    /// <summary>Die Ansicht, auf Station 4 gestellt.</summary>
    /// <param name="methode">Die gewählte Suchmethode (Auftrag #247, SD‑E‑10).</param>
    /// <param name="maximaleKandidaten">Die Grenze, gegen die die Kandidatenzeile prüft.</param>
    /// <param name="ausgleich">Der Energie-Ausgleichswert; <c>null</c> = 0,3.</param>
    /// <param name="hinweise">Die Hinweise der Vorprüfung.</param>
    /// <param name="variieren">Trägt die Einheit den Schalter „variieren"?</param>
    /// <param name="zweiteEinheit">Eine zweite Einheit samt Achse (Auftrag #273).</param>
    private IRenderedComponent<StromspeicherAuslegungSeite> Station(
        FlottenSuchmethode methode = FlottenSuchmethode.Groesse,
        int maximaleKandidaten = 10000, double? ausgleich = null,
        IReadOnlyList<FlottenHinweis>? hinweise = null, bool variieren = true,
        bool zweiteEinheit = false)
    {
        FlottenStudieKonfiguration flotte = Flotte(maximaleKandidaten, ausgleich);
        flotte.Auslegung.Suchmethode = methode;
        flotte.Auslegung.Achsen[0].Aktiv = variieren;

        if (zweiteEinheit)
        {
            var zweite = new FlottenEinheit
            {
                Id = "s2", Name = "Nebenspeicher", KapazitaetKWh = 60,
                LadeleistungKw = 30, EntladeleistungKw = 30,
                Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
            };
            flotte.Einheiten.Add(zweite);
            flotte.Auslegung.Achsen.Add(new FlottenAuslegungsAchse
            {
                Aktiv = variieren, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                AnzahlVon = 1, AnzahlBis = 2,
                KapazitaetVonKWh = 60, KapazitaetBisKWh = 120,
                LeistungVonKw = 30, LeistungBisKw = 60,
                Geraete = Bestand(),
                Vorlage = zweite
            });
        }

        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = flotte,
                    FlottenGroessenOptimieren = methode != FlottenSuchmethode.Bewerten
                }
            }
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                FlotteRechnen = (_, _) => Task.FromResult(new SpeicherFlottenErgebnis()),
                Vorpruefen = _ => hinweise ?? Array.Empty<FlottenHinweis>()
            })
            .Add(x => x.PlanerVerfuegbar, true)
            // OHNE ENTPRELLUNG: Die volle Vorpruefung laeuft im selben Zeichenlauf statt aus
            // einem Zeitgeber. Sonst meldet sich ihre Fortsetzung aus dem Fadenvorrat
            // zurueck, belegt den Zeichenverteiler und schiebt den naechsten Tastendruck
            // hinter die Pruefung — ein lastabhaengiger Ausreisser.
            .Add(x => x.EntprellungMs, 0));

        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Optimierung);
        return cut;
    }

    /// <summary>Die Ansicht nach einem Suchlauf mit drei Kandidaten.</summary>
    private IRenderedComponent<StromspeicherAuslegungSeite> Gerechnet(
        FlottenKandidatPhase phase = FlottenKandidatPhase.Fein)
    {
        FlottenKandidatZusammenfassung bester = Kandidat("K-30", 30, phase, 1500);
        var ergebnis = new SpeicherFlottenErgebnis
        {
            Erfolg = true,
            Auslegung = new FlottenAuslegungErgebnis
            {
                Kandidaten = new List<FlottenKandidatZusammenfassung>
                {
                    Kandidat("K-10", 10, FlottenKandidatPhase.Grob, 500),
                    Kandidat("K-20", 20, FlottenKandidatPhase.Grob, 900),
                    bester
                },
                BesterKandidat = bester,
                FeinrasterGerechnet = phase == FlottenKandidatPhase.Fein,
                Rechendauer = TimeSpan.FromSeconds(0.7)
            }
        };

        var vorgaben = new SpeicherOptimierungVorgaben
        {
            Eingaben = new SpeicherOptimierungEingaben
            {
                Auslegung = new SpeicherAuslegungKonfiguration
                {
                    Flotte = Flotte(10000, null),
                    FlottenGroessenOptimieren = true
                }
            }
        };

        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => vorgaben,
                FlotteRechnen = (_, _) => Task.FromResult(ergebnis)
            })
            .Add(x => x.PlanerVerfuegbar, true));

        Auslegungshilfe.Rechenknopf(cut).Click();
        // Auf den gezeichneten Lauf warten, nicht sofort pruefen: bunit gibt den Klick in
        // den Zeichenverteiler und kehrt zurueck, ohne den Zeichenlauf abzuwarten.
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance.Flottenergebnis));
        return cut;
    }

    private static FlottenKandidatZusammenfassung Kandidat(
        string id, double kapazitaet, FlottenKandidatPhase phase, double kapitalwert) => new()
    {
        KandidatId = id,
        Betriebsziel = FlottenBetriebsziel.PeakShaving,
        Zulaessig = true,
        Phase = phase,
        KapitalwertEuro = kapitalwert,
        KapazitaetKWh = kapazitaet,
        LadeleistungKw = kapazitaet / 2.0,
        EntladeleistungKw = kapazitaet / 2.0,
        ErsparnisEuroJahr = 180,
        Einheiten = new List<FlottenKandidatEinheit>
        {
            new()
            {
                Id = "s1",
                KapazitaetKWh = kapazitaet,
                LadeleistungKw = kapazitaet / 2.0,
                EntladeleistungKw = kapazitaet / 2.0
            }
        }
    };

    private static FlottenStudieKonfiguration Flotte(int maximaleKandidaten, double? ausgleich)
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1", Name = "Hauptspeicher", KapazitaetKWh = 100,
            LadeleistungKw = 50, EntladeleistungKw = 50,
            Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
            SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
        };
        return new FlottenStudieKonfiguration
        {
            Einheiten = new List<FlottenEinheit> { einheit },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving,
                EnergieAusgleichEuroProKWh = ausgleich ?? 0.3
            },
            Wirtschaftlichkeit = new FlottenWirtschaftlichkeitEingang
            {
                Kalkulationszins = 0.03, ProjektjahreBeiWiederholung = 20
            },
            Auslegung = new FlottenAuslegungEingang
            {
                MaximaleKandidaten = maximaleKandidaten,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    new()
                    {
                        Aktiv = true, Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        // Stueckzahl 1…3: Unter „Groesse suchen" steht sie fest auf 1
                        // (der Von-Wert), unter „Stueckzahl suchen" ergibt sie drei
                        // Kandidaten (Auftrag #247).
                        AnzahlVon = 1, AnzahlBis = 3,
                        KapazitaetVonKWh = 100, KapazitaetBisKWh = 300,
                        LeistungVonKw = 40, LeistungBisKw = 80,
                        // FUENF Geraete, VIER davon in beiden Bereichen.
                        Geraete = Bestand(),
                        Vorlage = einheit
                    }
                }
            }
        };
    }

    private static FlottenHinweis Hinweis(string text)
        => new() { Stufe = FlottenHinweisStufe.Hinweis, Text = text };

    /// <summary>
    /// Der Prüfbestand: fünf Geräte, von denen VIER in die Bereiche 100…300 kWh und
    /// 40…80 kW fallen; das fünfte (400 kWh / 100 kW) liegt daneben.
    /// </summary>
    private static List<FlottenGeraetekandidat> Bestand()
        => new[] { (100.0, 40.0), (150.0, 50.0), (200.0, 60.0), (300.0, 80.0), (400.0, 100.0) }
            .Select(g => new FlottenGeraetekandidat
            {
                Quellkennung = g.Item1.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                Geraet = new FlottenEinheit
                {
                    Id = "G" + g.Item1.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                    Name = "Speicher " + g.Item1.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                    KapazitaetKWh = g.Item1,
                    LadeleistungKw = g.Item2, EntladeleistungKw = g.Item2,
                    Ladewirkungsgrad = 0.95, Entladewirkungsgrad = 0.95,
                    SocMin = 0.1, SocMax = 0.9, SocStart = 0.5
                }
            }).ToList();

    private static IElement Suchwahl(IRenderedComponent<StromspeicherAuslegungSeite> cut, string text)
        => cut.FindAll("input[type=radio]")
              .Single(x => x.ParentElement!.TextContent.Contains(text, StringComparison.Ordinal));

    /// <summary>
    /// Die Zahlen- und Ganzzahlfelder der Einheitenkarte als KOMPONENTENinstanzen —
    /// die Prüfgröße des Befunds #245 (siehe dort, warum nicht die DOM-Knoten).
    /// </summary>
    private static IReadOnlyList<object> Zeilenkomponenten(
        IRenderedComponent<StromspeicherAuslegungSeite> cut)
        => cut.FindComponents<Ganzzahlfeld>().Select(x => (object)x.Instance)
              .Concat(cut.FindComponents<Zahlenfeld>().Select(x => (object)x.Instance))
              .ToList();

    /// <summary>Das Eingabefeld der Einheitenkarte mit dieser Beschriftung.</summary>
    private static IElement Kartenfeld(
        IRenderedComponent<StromspeicherAuslegungSeite> cut, string beschriftung)
        => cut.Find("article.epos-flotte-einheitskarte")
              .QuerySelectorAll("label.epos-feld")
              .Single(x => x.TextContent.Contains(beschriftung, StringComparison.Ordinal))
              .QuerySelector("input")!;
}
