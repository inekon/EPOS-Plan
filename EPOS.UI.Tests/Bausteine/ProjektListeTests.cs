using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Der Baustein <c>ProjektListe</c> (iU9-W15a.1) — die EINE Projektliste, die vier
/// Listen des Bestands abloest (Befund W15a-B52).
///
/// <para>Geprueft wird, was beim Port verloren gehen koennte: die Suche ueber die
/// UNSICHTBARE Beschreibung (W15a-B22), die Gleichstandsaufloesung ueber den Namen
/// (W15a-B20/ProjektAuswahl:332), die Zaehlzeile als Formatstring (W15a-B20) und die
/// zwei Betriebsarten der beiden Wirte — <c>NurName</c> und <c>AutoVorauswahl</c>.</para>
///
/// <para>Die Kultur ist auf de-DE gepinnt: Die Erwartungswerte sind deutsche
/// Beschriftungen und deutsche Datumsschreibweise.</para>
/// </summary>
public class ProjektListeTests : BunitContext
{
    private static readonly ProjektKopfZeile[] VIER =
    {
        new ProjektKopfZeile(1030, "Referenz BHKW", "Stadtwerke", "Kaskade mit drei Modulen",
                             new DateTime(2026, 3, 1)),
        new ProjektKopfZeile(1007, "Laurentiuskirche", "Kirchengemeinde", "Denkmalschutz",
                             new DateTime(2026, 5, 4)),
        new ProjektKopfZeile(1017, "Speicherhaus", "Stadtwerke", "PV mit Batterie",
                             new DateTime(2026, 5, 4)),
        new ProjektKopfZeile(1041, "Alte Mühle", "", "", null)
    };

    public ProjektListeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
    }

    /// <summary>
    /// Die Sprache der Oberflaeche wird auf de-DE gepinnt (Muster
    /// <c>GebaeudeKatalogDialogTests</c>) — Kultur UND Thread-Kultur, damit ein Lauf
    /// unter <c>LANG=en_US.UTF-8</c> dieselben Beschriftungen und dieselbe
    /// Datumsschreibweise sieht.
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

    private IRenderedComponent<ProjektListe> Aufbauen(Action<ComponentParameterCollectionBuilder<ProjektListe>>? mehr = null)
        => Render<ProjektListe>(p =>
        {
            p.Add(x => x.Zeilen, VIER);
            mehr?.Invoke(p);
        });

    [Fact]
    public void Drei_Spalten_eine_Zeile_je_Projekt_und_die_Zaehlzeile()
    {
        var cut = Aufbauen();

        // Wahlspalte + Projektname + Kunde + Geändert
        var koepfe = cut.FindAll("thead th");
        Assert.Equal(4, koepfe.Count);
        Assert.Contains("Projektname", koepfe[1].TextContent);
        Assert.Contains("Kunde", koepfe[2].TextContent);
        Assert.Contains("Geändert", koepfe[3].TextContent);

        Assert.Equal(4, cut.FindAll("tbody tr").Count);
        Assert.Equal("4 von 4 Projekten", cut.Find(".epos-projektliste-anzahl").TextContent);
    }

    [Fact]
    public void Die_Suche_greift_auch_ueber_die_unsichtbare_Beschreibung()
    {
        var cut = Aufbauen();

        // „Denkmalschutz" steht NUR in der Beschreibung - sie hat keine Spalte.
        cut.Find(".epos-projektliste-suche input").Input("Denkmalschutz");

        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Contains("Laurentiuskirche", cut.Find("tbody").TextContent);
        Assert.Equal("1 von 4 Projekten", cut.Find(".epos-projektliste-anzahl").TextContent);
        Assert.DoesNotContain("Denkmalschutz", cut.Find("tbody").TextContent);
    }

    [Fact]
    public void Die_Suche_greift_ueber_Name_und_Kunde()
    {
        var cut = Aufbauen();

        cut.Find(".epos-projektliste-suche input").Input("stadtwerke");
        Assert.Equal(2, cut.FindAll("tbody tr").Count);

        cut.Find(".epos-projektliste-suche input").Input("mühle");
        Assert.Single(cut.FindAll("tbody tr"));
    }

    [Fact]
    public void Ohne_Treffer_steht_der_Leertext_statt_einer_Tabelle()
    {
        var cut = Aufbauen(p => p.Add(x => x.LeerText, "Nichts gefunden."));

        cut.Find(".epos-projektliste-suche input").Input("gibt es nicht");

        Assert.Empty(cut.FindAll("table"));
        Assert.Contains("Nichts gefunden.", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Sortiert_wird_nach_Namen_und_der_Gleichstand_ueber_den_Namen()
    {
        var cut = Aufbauen();

        Assert.Equal(new[] { "Alte Mühle", "Laurentiuskirche", "Referenz BHKW", "Speicherhaus" },
                     Namen(cut));

        // Nach Datum: die beiden 04.05.2026 stehen in NAMENSREIHENFOLGE zueinander -
        // sonst spraenge die Liste bei jedem Aufbau (ProjektAuswahl:332).
        cut = Aufbauen(p => p.Add(x => x.SortSpalte, ProjektListe.SPALTE_GEAENDERT));
        string[] namen = Namen(cut);
        Assert.Equal("Alte Mühle", namen[0]);              // ohne Datum = kleinster Wert
        Assert.Equal("Referenz BHKW", namen[1]);
        Assert.Equal("Laurentiuskirche", namen[2]);
        Assert.Equal("Speicherhaus", namen[3]);
    }

    [Fact]
    public void Ein_Klick_auf_den_Spaltenkopf_dreht_die_Richtung()
    {
        var cut = Aufbauen();

        cut.FindAll("thead .epos-raster-kopf")[0].Click();   // Projektname, schon aktiv -> absteigend

        Assert.Equal(new[] { "Speicherhaus", "Referenz BHKW", "Laurentiuskirche", "Alte Mühle" },
                     Namen(cut));
    }

    [Fact]
    public void Die_schmale_Sicht_des_Assistenten_zeigt_nur_den_Namen()
    {
        var cut = Aufbauen(p => p.Add(x => x.NurName, true));

        var koepfe = cut.FindAll("thead th");
        Assert.Equal(2, koepfe.Count);                      // Wahlspalte + Projektname
        Assert.Contains("Projektname", koepfe[1].TextContent);
    }

    [Fact]
    public void Ohne_Vorauswahl_bleibt_nichts_markiert()
    {
        ProjektKopfZeile? gemeldet = null;

        var cut = Aufbauen(p => p
            .Add(x => x.AutoVorauswahl, false)
            .Add(x => x.MarkiertChanged, (ProjektKopfZeile? z) => gemeldet = z));

        Assert.Null(gemeldet);
        Assert.Empty(cut.FindAll("tr.epos-zeile--markiert"));
    }

    [Fact]
    public void Mit_Vorauswahl_ist_die_erste_Zeile_markiert()
    {
        ProjektKopfZeile? gemeldet = null;

        var cut = Aufbauen(p => p.Add(x => x.MarkiertChanged, (ProjektKopfZeile? z) => gemeldet = z));

        Assert.NotNull(gemeldet);
        Assert.Equal("Alte Mühle", gemeldet!.Name);
        Assert.Single(cut.FindAll("tr.epos-zeile--markiert"));
    }

    [Fact]
    public void Ein_Klick_auf_den_Wahlknopf_meldet_die_Zeile()
    {
        ProjektKopfZeile? gemeldet = null;

        var cut = Aufbauen(p => p
            .Add(x => x.AutoVorauswahl, false)
            .Add(x => x.MarkiertChanged, (ProjektKopfZeile? z) => gemeldet = z));

        cut.FindAll("tbody .epos-anlagenwahl")[2].Click();   // sortiert: Referenz BHKW

        Assert.NotNull(gemeldet);
        Assert.Equal(1030, gemeldet!.Id);
    }

    [Fact]
    public void Ein_Doppelklick_uebernimmt_die_Zeile()
    {
        ProjektKopfZeile? gewaehlt = null;

        var cut = Aufbauen(p => p.Add(x => x.Gewaehlt, (ProjektKopfZeile z) => gewaehlt = z));

        cut.FindAll("tbody tr")[1].DoubleClick();            // sortiert: Laurentiuskirche

        Assert.NotNull(gewaehlt);
        Assert.Equal(1007, gewaehlt!.Id);
    }

    [Fact]
    public void Der_Spaltensatz_des_Einstiegs_zeigt_Nummer_Klimaregion_und_Ausstattung()
    {
        var zeilen = new[]
        {
            new ProjektKopfZeile(1030, "B3-Kaskade", Klimazone: "Region 12", Ausstattung: "WP+BHKW")
        };

        var cut = Render<ProjektListe>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Spalten, ProjektListe.Spaltensatz.Einstieg)
            .Add(x => x.MitSuche, false)
            .Add(x => x.MitZaehler, false)
            .Add(x => x.MitAuswahl, false));

        var koepfe = cut.FindAll("thead th");
        Assert.Equal(4, koepfe.Count);
        Assert.Contains("Nr.", koepfe[0].TextContent);
        Assert.Contains("Projektname", koepfe[1].TextContent);
        Assert.Contains("Klimaregion", koepfe[2].TextContent);
        Assert.Contains("Ausstattung", koepfe[3].TextContent);

        string zeile = cut.Find("tbody tr").TextContent;
        Assert.Contains("1030", zeile);
        Assert.Contains("Region 12", zeile);
        Assert.Contains("WP+BHKW", zeile);

        Assert.Empty(cut.FindAll(".epos-projektliste-suche"));
        Assert.Empty(cut.FindAll(".epos-projektliste-anzahl"));
    }

    [Fact]
    public void Der_Zaehlsatz_ist_uebersetzbar()
    {
        var cut = Aufbauen(p => p.Add(x => x.AnzahlFormat, "{0} of {1} projects"));

        Assert.Equal("4 of 4 projects", cut.Find(".epos-projektliste-anzahl").TextContent);
    }

    [Fact]
    public void Das_Aenderungsdatum_steht_kurz_und_leer_wenn_keines_da_ist()
    {
        var cut = Aufbauen();

        var zellen = cut.FindAll("tbody tr")[2].QuerySelectorAll("td");  // Referenz BHKW
        Assert.Equal("01.03.2026", zellen[3].TextContent);

        var ohne = cut.FindAll("tbody tr")[0].QuerySelectorAll("td");    // Alte Mühle
        Assert.Equal("", ohne[3].TextContent);
    }

    // =====================================================================
    //  Die drei Spalten - Windows-Abnahme 05.09.2026, Befund W15a-B-1
    // =====================================================================

    /// <summary>
    /// <b>Befund W15a‑B‑1:</b> „Geändert Datum nicht ersichtlich."
    ///
    /// <para>Die Hausregel <c>.epos-raster td { white-space: nowrap }</c> lässt einen
    /// langen Projektnamen die Tabelle breiter machen, als die Spalte des Wirtes ist;
    /// in „Speichern unter" quetschte das Formular rechts die Liste auf rund 535 px,
    /// und die dritte Spalte lag hinter dem waagerechten Rollbalken. Jede Spalte trägt
    /// deshalb ihre Stilklasse — Name und Kunde brechen um, das Datum bleibt einzeilig
    /// mit fester Breite.</para>
    /// </summary>
    [Fact]
    public void Jede_Spalte_der_Auswahl_traegt_ihre_Stilklasse()
    {
        var cut = Aufbauen();

        var koepfe = cut.FindAll("thead th");
        Assert.Equal("", koepfe[0].ClassName?.Replace("epos-projektliste-wahl", "").Trim());
        Assert.Contains("epos-projektliste-name", koepfe[1].ClassName ?? "");
        Assert.Contains("epos-projektliste-kunde", koepfe[2].ClassName ?? "");
        Assert.Contains("epos-projektliste-geaendert", koepfe[3].ClassName ?? "");

        var zellen = cut.FindAll("tbody tr")[0].QuerySelectorAll("td");
        Assert.Contains("epos-projektliste-name", zellen[1].ClassName ?? "");
        Assert.Contains("epos-projektliste-kunde", zellen[2].ClassName ?? "");
        Assert.Contains("epos-projektliste-geaendert", zellen[3].ClassName ?? "");
    }

    /// <summary>
    /// Die Regel dahinter — eine bunit-Probe sieht ein Stilblatt nicht (Lehre W6‑B‑1).
    /// Name und Kunde brechen um, das Datum bleibt in EINER Zeile: Es ist die
    /// kürzeste Spalte und die einzige, deren Umbruch nichts brächte.
    /// </summary>
    [Fact]
    public void Name_und_Kunde_brechen_um_das_Datum_nicht()
    {
        string umbrechend = Stilblock(
            ".epos-projektliste-raster .epos-projektliste-name,\n" +
            ".epos-projektliste-raster .epos-projektliste-kunde {");
        Assert.Contains("white-space: normal", umbrechend, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: anywhere", umbrechend, StringComparison.Ordinal);

        string datum = Stilblock(".epos-projektliste-raster .epos-projektliste-geaendert {");
        Assert.Contains("white-space: nowrap", datum, StringComparison.Ordinal);
        Assert.Contains("width:", datum, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Anwenderwunsch 05.09.2026 (W15a‑E‑1), zweite Haelfte:</b> „der Name ist
    /// abgeschnitten".
    ///
    /// <para>Die Umbruchregel stand seit W15a‑B‑1 im Blatt und wirkte NICHT:
    /// <c>.epos-raster td</c> ist (0,1,1), <c>.epos-projektliste-name</c> nur
    /// (0,1,0) — die Hausregel gewann jedes Mal. Sichtbar wurde es erst im 280 px
    /// breiten Assistentenband, wo der waagerechte Rollbalken genau den Teil des
    /// Namens abschnitt, der eine Variante ausmacht.</para>
    ///
    /// <para>Geprueft wird die SPEZIFITAET, nicht der Wortlaut: Der Selektor der
    /// Projektliste muss mehr Klassen fuehren als die Hausregel. Eine bunit-Probe
    /// sieht das nicht (Lehre W6‑B‑1).</para>
    /// </summary>
    [Fact]
    public void Die_Umbruchregel_schlaegt_die_Hausregel_des_Rasters()
    {
        string css = Stilblatt();

        // Die Hausregel steht weiter da - sie ist fuer kurze Bezeichner richtig.
        Assert.Contains(".epos-raster th,\n.epos-raster td {", css, StringComparison.Ordinal);

        // ... und die Projektliste hebt sie mit EINER Klasse mehr auf, nicht mit
        // einer Wichtigkeitsmarke.
        Assert.Contains(".epos-projektliste-raster .epos-projektliste-name,", css,
                        StringComparison.Ordinal);
        Assert.DoesNotContain("white-space: normal !important", css, StringComparison.Ordinal);
    }

    /// <summary>
    /// „Speichern unter" stellt Liste und Formular nebeneinander. Bis zum Befund
    /// brach das erst bei 780 px um — im 940‑px‑Fenster stand das Formular also
    /// neben der Liste und ließ ihr drei Fünftel. Ab hier steht die Liste über die
    /// volle Breite und das Formular darunter: bei 1 024 px, im 940‑px‑Dialog und
    /// auf dem iPad hochkant.
    /// </summary>
    [Fact]
    public void Speichern_unter_stapelt_Liste_und_Formular_bis_1100_Pixel()
    {
        string css = Stilblatt();

        int a = css.IndexOf("@media (max-width: 1100px) {", StringComparison.Ordinal);
        Assert.True(a >= 0, "Der Umbruch von \"Speichern unter\" steht nicht bei 1100 px");

        int e = css.IndexOf("}\n}", a, StringComparison.Ordinal);
        Assert.True(e > a);
        Assert.Contains(".epos-projektkopie-raster", css.Substring(a, e - a), StringComparison.Ordinal);

        // Und die frühere, zu späte Schwelle steht nicht mehr da.
        Assert.DoesNotContain("@media (max-width: 780px)", css, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Varianten - Anwenderwunsch 05.09.2026, W15a-E-1
    // =====================================================================

    /// <summary>
    /// Die Gruppe der Testdatenbank: „Woehler" mit den zwei Varianten „Test1" und
    /// „Test2", dazu ein gewoehnliches Projekt und ein zweites, dessen Name dem
    /// Stamm zum Verwechseln aehnelt — genau die Lage des Bildschirmfotos.
    /// </summary>
    private static readonly ProjektKopfZeile[] GRUPPE =
    {
        new ProjektKopfZeile(1024, "Wöhler - Test2", "Stadtwerke", "", new DateTime(2026, 4, 2),
                             StammId: 1019, Bezeichner: "Test2", StammName: "Wöhler"),
        new ProjektKopfZeile(1019, "Wöhler", "Stadtwerke", "Stammprojekt", new DateTime(2026, 4, 1)),
        new ProjektKopfZeile(1023, "Wöhler - Test1", "Stadtwerke", "", new DateTime(2026, 4, 3),
                             StammId: 1019, Bezeichner: "Test1", StammName: "Wöhler"),
        new ProjektKopfZeile(1030, "Wöhler WP", "Kirchengemeinde", "", new DateTime(2026, 1, 9)),
        new ProjektKopfZeile(1007, "Alte Mühle", "", "", null)
    };

    private IRenderedComponent<ProjektListe> MitGruppe(
        Action<ComponentParameterCollectionBuilder<ProjektListe>>? mehr = null)
        => Render<ProjektListe>(p =>
        {
            p.Add(x => x.Zeilen, GRUPPE);
            mehr?.Invoke(p);
        });

    /// <summary>
    /// Die Spalte „Art" nennt Stamm und Variante samt Bezeichner — und steht
    /// zwischen Name und Kunde, weil sie den NAMEN qualifiziert.
    /// </summary>
    [Fact]
    public void Die_Artspalte_nennt_Stamm_Variante_und_den_Bezeichner()
    {
        var cut = MitGruppe();

        var koepfe = cut.FindAll("thead th");
        Assert.Equal(5, koepfe.Count);                       // Wahl + Name + Art + Kunde + Geändert
        Assert.Contains("Art", koepfe[2].TextContent);
        Assert.Contains("epos-projektliste-artspalte", koepfe[2].ClassName ?? "");

        // Zeile 1 ist der Stamm, Zeile 2 und 3 seine Varianten (siehe Sortierfall).
        var zeilen = cut.FindAll("tbody tr");
        Assert.Equal("Stamm", zeilen[1].QuerySelectorAll("td")[2].TextContent.Trim());

        string variante = zeilen[2].QuerySelectorAll("td")[2].TextContent;
        Assert.Contains("Variante", variante);
        Assert.Contains("Test1", variante);
    }

    /// <summary>
    /// Ein Projekt, an dem keine Variante haengt, ist WEDER Stamm NOCH Variante —
    /// seine Artzelle bleibt leer. „Stamm" ueber alle 24 Zeilen zu schreiben waere
    /// eine Auskunft, die der Bestand nie gegeben hat.
    /// </summary>
    [Fact]
    public void Ein_gewoehnliches_Projekt_traegt_keine_Art()
    {
        var cut = MitGruppe();

        var zeilen = cut.FindAll("tbody tr");
        Assert.Equal("Alte Mühle", zeilen[0].QuerySelectorAll("td")[1].TextContent.Trim());
        Assert.Equal("", zeilen[0].QuerySelectorAll("td")[2].TextContent.Trim());
    }

    /// <summary>
    /// Ohne Variante in der Liste gibt es die Spalte gar nicht — sie waere in jeder
    /// Zeile leer und naehme dem Namen Platz weg.
    /// </summary>
    [Fact]
    public void Ohne_Variante_erscheint_die_Artspalte_nicht()
    {
        var cut = Aufbauen();

        Assert.Equal(4, cut.FindAll("thead th").Count);
        Assert.DoesNotContain("Art", cut.Find("thead").TextContent);
    }

    /// <summary>
    /// Die Ordnung des Vorbilds (<c>VariantenCtrl.LadeGruppe</c>,
    /// <c>Form_Start.FuelleVariantenCombo</c>): der Stamm zuerst, darunter seine
    /// Varianten nach BEZEICHNER — nicht nach Projektname und nicht nach Datum.
    /// </summary>
    [Fact]
    public void Jede_Variante_steht_unter_ihrem_Stamm_nach_Bezeichner()
    {
        var cut = MitGruppe();

        Assert.Equal(new[] { "Alte Mühle", "Wöhler", "Wöhler - Test1", "Wöhler - Test2", "Wöhler WP" },
                     Namen(cut));

        // Auch nach Datum sortiert bleibt die Gruppe beieinander: Die Spalte ordnet
        // die STAEMME, innerhalb der Gruppe gilt weiter der Bezeichner.
        cut = MitGruppe(p => p.Add(x => x.SortSpalte, ProjektListe.SPALTE_GEAENDERT));
        string[] namen = Namen(cut);
        Assert.Equal("Alte Mühle", namen[0]);                 // ohne Datum
        Assert.Equal("Wöhler WP", namen[1]);                  // 09.01.2026
        Assert.Equal("Wöhler", namen[2]);                     // 01.04.2026
        Assert.Equal("Wöhler - Test1", namen[3]);
        Assert.Equal("Wöhler - Test2", namen[4]);
    }

    /// <summary>
    /// Die Variantenzeile ist auch OHNE Artspalte zu erkennen: eingerueckt und mit
    /// der leisen Herkunftszeile. Das ist der Fall des Assistentenbandes.
    /// </summary>
    [Fact]
    public void Im_schmalen_Band_traegt_die_Variante_Einrueckung_und_Herkunft()
    {
        var cut = MitGruppe(p => p.Add(x => x.NurName, true));

        Assert.Equal(2, cut.FindAll("thead th").Count);       // Wahl + Projektname, keine Artspalte

        var zeilen = cut.FindAll("tbody tr");
        Assert.Contains("epos-projektliste-zeile--variante", zeilen[2].ClassName ?? "");
        Assert.DoesNotContain("epos-projektliste-zeile--variante", zeilen[1].ClassName ?? "");

        var eintrag = zeilen[2].QuerySelector(".epos-projektliste-eintrag");
        Assert.NotNull(eintrag);
        Assert.Contains("epos-projektliste-eintrag--variante", eintrag!.ClassName ?? "");

        Assert.Equal("Variante von Wöhler",
                     zeilen[2].QuerySelector(".epos-projektliste-herkunft")!.TextContent);

        // Der Stamm traegt keine.
        Assert.Null(zeilen[1].QuerySelector(".epos-projektliste-herkunft"));
    }

    /// <summary>
    /// Wo die Artspalte steht, sagt sie dieselbe Sache — die leise Zeile bliebe
    /// dieselbe Auskunft zweimal.
    /// </summary>
    [Fact]
    public void Mit_Artspalte_entfaellt_die_leise_Herkunftszeile()
    {
        var cut = MitGruppe();

        Assert.Empty(cut.FindAll(".epos-projektliste-herkunft"));
        Assert.NotEmpty(cut.FindAll(".epos-projektliste-art"));
    }

    /// <summary>
    /// Der Bezeichner ist NIRGENDS eine eigene Spalte des Assistenten und steht in
    /// der Auswahl klein unter dem Wort „Variante" — gesucht werden muss er
    /// trotzdem, sonst findet ihn niemand (dieselbe Lehre wie die unsichtbare
    /// Beschreibung, W15a‑B22).
    /// </summary>
    [Fact]
    public void Die_Suche_greift_auch_ueber_den_Variantenbezeichner()
    {
        var cut = MitGruppe();

        cut.Find(".epos-projektliste-suche input").Input("Test2");

        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Equal("Wöhler - Test2", Namen(cut)[0]);
    }

    /// <summary>
    /// Faellt der Stamm durch den Filter, steht die Variante SELBST oben — sonst
    /// waere sie nach einer Suche unauffindbar.
    /// </summary>
    [Fact]
    public void Ohne_ihren_Stamm_steht_die_Variante_selbst_oben()
    {
        var cut = MitGruppe();

        cut.Find(".epos-projektliste-suche input").Input("Test");

        Assert.Equal(new[] { "Wöhler - Test1", "Wöhler - Test2" }, Namen(cut));
        Assert.Equal("2 von 5 Projekten", cut.Find(".epos-projektliste-anzahl").TextContent);
    }

    /// <summary>
    /// Ohne Stammnamen — der Stamm ist geloescht, oder die Datenbank kennt
    /// <c>Tab_Variante</c> nicht — bleibt das blosse Wort stehen. „Variante von "
    /// ohne Namen waere ein angefangener Satz.
    /// </summary>
    [Fact]
    public void Ohne_Stammnamen_bleibt_das_blosse_Wort()
    {
        var zeilen = new[]
        {
            new ProjektKopfZeile(1023, "Waise", StammId: 999, Bezeichner: "X")
        };

        var cut = Render<ProjektListe>(p => p
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.NurName, true));

        Assert.Equal("Variante", cut.Find(".epos-projektliste-herkunft").TextContent);
    }

    /// <summary>
    /// Die Einrueckung ist eine REGEL, kein Markup — eine bunit-Probe sieht sie
    /// nicht (Lehre W6‑B‑1).
    /// </summary>
    [Fact]
    public void Die_Variantenzeile_ist_im_Stilblatt_eingerueckt()
    {
        string block = Stilblock(".epos-projektliste-eintrag--variante {");
        Assert.Contains("padding-inline-start", block, StringComparison.Ordinal);
        Assert.Contains("border-inline-start", block, StringComparison.Ordinal);

        string herkunft = Stilblock(".epos-projektliste-herkunft {");
        Assert.Contains("var(--epos-text-leise)", herkunft, StringComparison.Ordinal);
    }

    private static string[] Namen(IRenderedComponent<ProjektListe> cut)
        => cut.FindAll("tbody tr")
              .Select(r => r.QuerySelectorAll("td")[1].TextContent)
              .ToArray();

    /// <summary>Das Hausblatt, Zeilenenden angeglichen (Muster <c>StartseiteTests</c>).</summary>
    private static string Stilblatt()
    {
        System.IO.DirectoryInfo? d = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null &&
               !System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return System.IO.File
                     .ReadAllText(System.IO.Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))
                     .Replace("\r\n", "\n");
    }

    /// <summary>Liest den Rumpf einer Regel aus dem Hausblatt.</summary>
    private static string Stilblock(string selektor)
    {
        string css = Stilblatt();
        selektor = selektor.Replace("\r\n", "\n");

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, "Regel " + selektor + " steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }

    // =====================================================================
    // Merge 5 (Nutzerauftrag 02.09.2026): Varianten unter ihrem Stamm, Mehrfachauswahl
    // =====================================================================

    private static readonly ProjektKopfZeile[] MIT_STAMM =
    {
        new ProjektKopfZeile(1030, "Referenz BHKW", "Stadtwerke", "Kaskade", new DateTime(2026, 3, 1)),
        new ProjektKopfZeile(1031, "Referenz BHKW - V2", "Stadtwerke", "Variante zwei", new DateTime(2026, 3, 2),
                             StammId: 1030, Bezeichner: "V2", StammName: "Referenz BHKW"),
        new ProjektKopfZeile(1007, "Laurentiuskirche", "Kirchengemeinde", "Denkmalschutz", new DateTime(2026, 5, 4)),
        new ProjektKopfZeile(1032, "Referenz BHKW - V1", "Stadtwerke", "Variante eins", new DateTime(2026, 3, 3),
                             StammId: 1030, Bezeichner: "V1", StammName: "Referenz BHKW")
    };

    /// <summary>Die Zeile traegt die Beschreibung als Tooltip (Nutzerauftrag 02.09.2026).</summary>
    [Fact]
    public void Die_Zeile_traegt_die_Beschreibung_als_Tooltip()
    {
        var cut = Render<ProjektListe>(p => p.Add(x => x.Zeilen, MIT_STAMM));
        Assert.Equal("Denkmalschutz", cut.FindAll("tbody tr")[0].GetAttribute("title"));   // sortiert: Laurentiuskirche
    }

    /// <summary>Im Mehrfachmodus gibt es Haekchen statt Einzelwahl; ein Stamm nimmt seine Varianten mit.</summary>
    [Fact]
    public void Ein_Haken_am_Stamm_nimmt_die_Varianten_mit()
    {
        IReadOnlyCollection<int> angehakt = Array.Empty<int>();
        var cut = Render<ProjektListe>(p => p
            .Add(x => x.Zeilen, MIT_STAMM)
            .Add(x => x.Mehrfach, true)
            .Add(x => x.Angehakt, angehakt)
            .Add(x => x.AngehaktChanged, s => angehakt = s));

        var haken = cut.FindAll(".epos-projektliste-haken");
        Assert.Equal(4, haken.Count);
        Assert.Empty(cut.FindAll(".epos-anlagenwahl"));

        haken[1].Change(true);                                   // Zeile 2 = Stamm "Referenz BHKW"
        Assert.Equal(new[] { 1030, 1031, 1032 }, angehakt.OrderBy(i => i).ToArray());
    }
}
