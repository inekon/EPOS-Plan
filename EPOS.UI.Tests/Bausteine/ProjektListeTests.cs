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

    private static string[] Namen(IRenderedComponent<ProjektListe> cut)
        => cut.FindAll("tbody tr")
              .Select(r => r.QuerySelectorAll("td")[1].TextContent)
              .ToArray();
}
