using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Spalten nach Rang — keine waagerechte Rollleiste</b> (Konzept
/// Administrationsdialoge, Stufe 1, Vorschläge <b>V2</b> und <b>V7</b>).
///
/// <para><b>Der Befund</b> (Katalogprobe, 1 088 × 624, vor Stufe 1): Die Liste des
/// Heizkessels rollte um 234 px quer, die der Wärmepumpe um 1 269 px. Jetzt weichen
/// Spalten mit Rang, der Bezeichner ist elastisch und kürzt, und eine gefilterte
/// oder sortierte Spalte weicht nie.</para>
///
/// <para><b>Dreierlei wird geprüft</b>: die RECHNUNG (<see cref="Spaltenraenge"/>, ohne
/// Oberfläche), das MARKUP der <c>Katalogliste</c> (bunit — Klassen und Kurztexte) und
/// die REGELN im Stilblatt. Ob die Liste im Browser wirklich nicht quer rollt, misst
/// <c>Proben/Rasterprobe/katalogprobe.mjs</c> (Fälle N, 1 088 × 624 und 400 × 624);
/// bunit misst keine Breite.</para>
/// </summary>
public class SpaltenraengeTests : EposBunitContext
{
    public SpaltenraengeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =====================================================================
    //  Die Rechnung
    // =====================================================================

    [Theory]
    [InlineData(0, 400)]
    [InlineData(400, 400)]
    [InlineData(401, 480)]
    [InlineData(1054, 1120)]
    [InlineData(99999, 2400)]
    public void Die_Leiter_rundet_auf_die_naechste_Stufe(int breite, int stufe)
    {
        Assert.Equal(stufe, Spaltenraenge.Stufe(breite));
    }

    /// <summary>
    /// <b>Bezeichner und EIN Hauptkennwert stehen immer</b> — in jedem Profil der
    /// Verwaltungen (die acht Anlagenkataloge, die drei Bedarfe, die drei Zeitreihen).
    /// Mehr als einer passte bei 400 px nicht neben einen lesbaren Bezeichner.
    /// </summary>
    [Fact]
    public void Jede_Verwaltung_zeigt_immer_Bezeichner_und_genau_einen_Kennwert()
    {
        var profile = Katalogfilterprofil.AlleArten.Select(a => Katalogfilterprofil.Finde(a))
            .Concat(new[] { BedarfsArt.Brauchwasser, BedarfsArt.Prozesswaerme, BedarfsArt.Stromverbraucher }
                .Select(a => Katalogfilterprofil.FuerBedarf(a)))
            .Concat(new[] { Zeitreihenart.Waermebedarf, Zeitreihenart.Stromganglinie, Zeitreihenart.Solarganglinie }
                .Select(a => Katalogfilterprofil.FuerZeitreihe(a)))
            .ToList();

        Assert.Equal(14, profile.Count);
        Assert.All(profile, p =>
        {
            Assert.True(Spaltenraenge.HatRaenge(p), p.Schluessel);
            Assert.Equal(Katalogspaltenrang.Immer, p.Spalte(Katalogfilterprofil.SpBezeichner)!.Rang);
            Assert.Single(p.Spalten, s => s.Rang == Katalogspaltenrang.Immer
                                      && s.Schluessel != Katalogfilterprofil.SpBezeichner);
        });
    }

    /// <summary>Ein Profil ohne Rang (Importe, Flotte, Gesetze) zeigt alle Spalten wie bisher.</summary>
    [Fact]
    public void Ohne_Rang_weicht_keine_Spalte()
    {
        var profil = Katalogfilterprofil.AusSpalten("PROBE", new[]
        {
            new Katalogspalte("A", "A"),
            new Katalogspalte("B", "B", "", Katalogspaltenart.Zahl)
        });

        Assert.False(Spaltenraenge.HatRaenge(profil));
        Assert.All(Spaltenraenge.Stufen(profil, new Dictionary<string, int>(), _ => false).Values,
                   s => Assert.Equal(0, s));
    }

    /// <summary>
    /// <b>Breit weicht vor BeiPlatz</b>, und innerhalb eines Ranges die spätere Spalte
    /// vor der früheren: Wird die Liste schmaler, geht zuerst der Brennwert, dann η,
    /// dann der Brennstoff, zuletzt der Hersteller. P_th (Rang Immer) weicht nie.
    /// </summary>
    [Fact]
    public void Die_Stufen_folgen_dem_Rang()
    {
        var p = Kessel();
        var stufen = Spaltenraenge.Stufen(p, Laengen(), _ => false);

        Assert.Equal(0, stufen[Katalogfilterprofil.SpBezeichner]);
        Assert.Equal(0, stufen[Katalogfilterprofil.SpPtherm]);
        Assert.True(stufen[Katalogfilterprofil.SpHersteller] > 0);
        Assert.True(stufen[Katalogfilterprofil.SpBrennstoff] >= stufen[Katalogfilterprofil.SpHersteller]);
        Assert.True(stufen[Katalogfilterprofil.SpEta] >= stufen[Katalogfilterprofil.SpBrennstoff]);
        Assert.True(stufen[Katalogfilterprofil.SpBrennwert] > stufen[Katalogfilterprofil.SpEta]);
    }

    /// <summary>
    /// <b>Im Fenster des Anwenders steht der Kessel ganz</b>: Bei 1 088 × 624 ist die
    /// Liste 1 056 px breit (gemessen), und mit Firmen von 34 Zeichen passen alle
    /// sieben Spalten — die Katalogprobe maß danach 0 px Querüberlauf. Bei 400 px
    /// (Liste 368 px) bleiben Bezeichner und P_th.
    /// </summary>
    [Fact]
    public void Im_Fenster_des_Anwenders_steht_der_Kessel_ganz_und_schmal_nur_das_Noetige()
    {
        var p = Kessel();
        var stufen = Spaltenraenge.Stufen(p, Laengen(), _ => false);

        Assert.All(stufen.Values, s => Assert.True(s <= 1056, s.ToString()));
        Assert.All(stufen.Where(s => s.Value > 0), s => Assert.True(s.Value > 368, s.Key));
    }

    /// <summary>
    /// <b>V7: Eine Spalte mit Filter oder Sortierung weicht nie</b> — und weil sie
    /// immer steht, zählt sie zur Grundbreite: Die übrigen weichenden Spalten brauchen
    /// danach mehr Platz, nicht weniger.
    /// </summary>
    [Fact]
    public void Eine_festgehaltene_Spalte_weicht_nie()
    {
        var p = Kessel();
        var frei = Spaltenraenge.Stufen(p, Laengen(), _ => false);
        var fest = Spaltenraenge.Stufen(p, Laengen(), s => s.Schluessel == Katalogfilterprofil.SpBrennwert);

        Assert.Equal(0, fest[Katalogfilterprofil.SpBrennwert]);
        Assert.True(fest[Katalogfilterprofil.SpHersteller] >= frei[Katalogfilterprofil.SpHersteller]);
    }

    /// <summary>
    /// Eine Textspalte außer dem Bezeichner zählt höchstens mit ihrer Höchstbreite
    /// (14 rem = 224 px plus Polsterung) — ein Firmenname von 34 Zeichen nahm 250 px.
    /// </summary>
    [Fact]
    public void Eine_lange_Textspalte_zaehlt_hoechstens_mit_ihrer_Hoechstbreite()
    {
        var hersteller = Katalogfilterprofil.Finde(Anlagenart.Heizkessel).Spalte(Katalogfilterprofil.SpHersteller)!;

        Assert.True(Spaltenraenge.IstBegrenzt(hersteller));
        Assert.Equal(Spaltenraenge.TEXT_HOECHST + Spaltenraenge.ZELLE, Spaltenraenge.Breite(hersteller, 100));
        Assert.Equal(14 * 16, Spaltenraenge.TEXT_HOECHST);
    }

    /// <summary>Die Längen laufen über ALLE Zeilen und lassen den elastischen Bezeichner aus.</summary>
    [Fact]
    public void Die_Laengen_laufen_ueber_alle_Zeilen_ohne_Bezeichner()
    {
        var laengen = Spaltenraenge.Laengen(Katalogfilterprofil.Finde(Anlagenart.Heizkessel), Zeilen());

        Assert.False(laengen.ContainsKey(Katalogfilterprofil.SpBezeichner));
        Assert.Equal("Nordwerk Heiztechnik GmbH & Co. KG".Length, laengen[Katalogfilterprofil.SpHersteller]);
    }

    // =====================================================================
    //  Das Markup der Katalogliste
    // =====================================================================

    /// <summary>
    /// Die Liste ist nur dann ein Container der Abfragen, wenn ihr Profil einen Rang
    /// kennt — ein Container misst seine Breite nicht mehr am Inhalt, und die Wirte
    /// ohne Rang sollen bleiben, wie sie waren.
    /// </summary>
    [Fact]
    public void Nur_mit_Rang_ist_die_Liste_ein_Container()
    {
        Assert.Contains("epos-katalogliste--raenge",
                        Aufbauen().Find(".epos-katalogliste").ClassName ?? "");

        var ohne = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Katalogfilterprofil.AusSpalten("PROBE", new[] { new Katalogspalte("A", "A") }))
            .Add(x => x.Zeilen, new List<Katalogfilterzeile> { new Katalogfilterzeile(1, "x").MitText("A", "x") }));
        Assert.DoesNotContain("epos-katalogliste--raenge", ohne.Find(".epos-katalogliste").ClassName ?? "");
        Assert.Empty(ohne.FindAll("[class*='epos-spalte-ab-']"));
    }

    /// <summary>
    /// Jede Spalte trägt ihren Rang; eine weichende dazu die Stufe, ab der sie steht —
    /// an Kopf UND Zellen (ColumnBase.Class). Der Bezeichner ist elastisch, der
    /// Hersteller begrenzt.
    /// </summary>
    [Fact]
    public void Jede_Spalte_traegt_Rang_und_die_weichende_ihre_Stufe()
    {
        var cut = Aufbauen();
        var koepfe = cut.FindAll("thead th").Skip(1).ToList();          // ohne Wahlspalte

        string bez = koepfe[0].ClassName ?? "";
        Assert.Contains("epos-spalte--bezeichner", bez);
        Assert.Contains("epos-spalte-rang1", bez);
        Assert.DoesNotContain("epos-spalte-ab-", bez);

        string firma = koepfe[1].ClassName ?? "";
        Assert.Contains("epos-spalte--begrenzt", firma);
        Assert.Contains("epos-spalte-rang2", firma);
        Assert.Contains("epos-spalte-ab-", firma);

        string pth = koepfe[3].ClassName ?? "";
        Assert.Contains("epos-spalte-rang1", pth);
        Assert.DoesNotContain("epos-spalte-ab-", pth);

        Assert.Contains("epos-spalte-rang3", koepfe[5].ClassName ?? "");

        // Dieselbe Stufe steht an jeder Zelle der Spalte.
        string stufe = firma.Split(' ').Single(k => k.StartsWith("epos-spalte-ab-", StringComparison.Ordinal));
        Assert.Equal(3, cut.FindAll("tbody td." + stufe).Count);
    }

    /// <summary>
    /// <b>V7: Eine gefilterte oder sortierte Spalte wird nie ausgeblendet</b> — ihr
    /// gefüllter Trichter bzw. ihr Pfeil ist die einzige Anzeige dafür. Mit dem Filter
    /// verliert sie ihre Stufe, mit „Filter zurücksetzen" bekommt sie sie zurück.
    /// </summary>
    [Fact]
    public void Eine_gefilterte_oder_sortierte_Spalte_wird_nie_ausgeblendet()
    {
        var stand = new Katalogfilterstand();
        stand.Setzen(Katalogfilterprofil.SpHersteller, "nord");
        stand.Sortieren(Katalogfilterprofil.SpBrennwert);
        var cut = Aufbauen(stand);

        var koepfe = cut.FindAll("thead th").Skip(1).ToList();
        Assert.DoesNotContain("epos-spalte-ab-", koepfe[1].ClassName ?? "");   // Hersteller, gefiltert
        Assert.Contains("epos-spalte--gefiltert", koepfe[1].ClassName ?? "");
        Assert.DoesNotContain("epos-spalte-ab-", koepfe[5].ClassName ?? "");   // Brennwert, sortiert
        Assert.Contains("epos-spalte-ab-", koepfe[2].ClassName ?? "");         // Brennstoff weicht weiter

        cut.Find(".epos-katalog-ruecksetzer").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("epos-spalte-ab-", cut.FindAll("thead th")[2].ClassName ?? ""));
    }

    /// <summary>
    /// <b>Wer kürzt, trägt den vollen Wert im Kurztext</b>: der elastische Bezeichner
    /// und die begrenzten Textspalten. Zahlen kürzen nie und tragen keinen.
    /// </summary>
    [Fact]
    public void Gekuerzte_Zellen_tragen_den_vollen_Wert_im_Kurztext()
    {
        var zeile = Aufbauen().FindAll("tbody tr")[0];
        var zellen = zeile.QuerySelectorAll("td").Skip(1).ToList();     // ohne Wahlspalte

        Assert.Equal("Brennwertkessel Baureihe 2026 Plus mit modulierendem Brenner 3,8-24 kW Typ S",
                     zellen[0].QuerySelector("span")!.GetAttribute("title"));
        Assert.Equal("Nordwerk Heiztechnik GmbH & Co. KG", zellen[1].QuerySelector("span")!.GetAttribute("title"));
        Assert.Null(zellen[3].QuerySelector("span")!.GetAttribute("title"));   // P_th
    }

    /// <summary>
    /// Das Popover öffnet in der rechten Hälfte des Profils nach links. Seit Spalten
    /// weichen, steht rechts nicht mehr sicher die letzte des Profils — bei 400 px ist
    /// P_th die rechte Randspalte, und ein Popover nach rechts ragte aus der Liste.
    /// </summary>
    [Fact]
    public void Das_Popover_oeffnet_in_der_rechten_Haelfte_nach_links()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-trichter")[3].Click();                      // P_th (vierte Spalte von sechs)
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".epos-spaltenfilter--rechts")));

        cut.FindAll(".epos-trichter")[1].Click();                      // Hersteller
        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll(".epos-spaltenfilter"));
            Assert.Empty(cut.FindAll(".epos-spaltenfilter--rechts"));
        });
    }

    // =====================================================================
    //  Die Regeln im Stilblatt
    // =====================================================================

    /// <summary>
    /// <b>Die Leiter steht vollständig im Stilblatt</b> — je Stufe der Rechnung EINE
    /// Containerabfrage. Fehlte eine, bliebe jede Spalte mit dieser Stufe immer stehen.
    /// </summary>
    [Fact]
    public void Die_Leiter_steht_vollstaendig_im_Stilblatt()
    {
        string css = Stilblatt();
        for (int n = Spaltenraenge.ERSTE_STUFE; n <= Spaltenraenge.LETZTE_STUFE; n += Spaltenraenge.STUFE)
            Assert.Contains($"@container epos-katalogliste (max-width: {n - 1}.98px) {{ .epos-spalte-ab-{n} {{ display: none; }} }}",
                            css);
    }

    [Fact]
    public void Die_Liste_mit_Rang_ist_ein_Container_ihrer_Breite()
    {
        string block = Stilblock(".epos-katalogliste--raenge {");
        Assert.Contains("container-type: inline-size", block);
        Assert.Contains("container-name: epos-katalogliste", block);
    }

    /// <summary>
    /// Der Bezeichner nimmt den Rest der Zeile (width 100 %), darf darunter schrumpfen
    /// (max-width 0) und kürzt mit „…"; seine Kopfzelle hält 7 rem.
    /// </summary>
    [Fact]
    public void Der_Bezeichner_ist_elastisch_und_kuerzt()
    {
        // Zwei Regeln tragen denselben Zellselektor (width 100 % gemeinsam mit dem Kopf,
        // das Kuerzen nur an der Zelle) - gesucht wird die zweite am Inhalt.
        Assert.Matches(@"td\.epos-spalte--bezeichner \{\s*max-width: 0;\s*overflow: hidden;\s*text-overflow: ellipsis;",
                       Stilblatt());

        Assert.Contains("width: 100%", Stilblock(".epos-katalogliste table.epos-raster th.epos-spalte--bezeichner,"));
        Assert.Contains("min-width: 7rem", Stilblock(".epos-katalogliste table.epos-raster th.epos-spalte--bezeichner {"));
        Assert.Contains("contain: inline-size", Stilblock(".epos-katalogliste th.epos-spalte--bezeichner .epos-spaltenkopf {"));
    }

    [Fact]
    public void Begrenzte_Textspalten_kuerzen_bei_14_rem_und_Zahlen_stehen_rechts()
    {
        string block = Stilblock(".epos-katalogliste td.epos-spalte--begrenzt > span {");
        Assert.Contains("max-width: 14rem", block);
        Assert.Contains("text-overflow: ellipsis", block);

        Assert.Contains("text-align: right", Stilblock(".epos-katalogliste table.epos-raster td.epos-spalte-zahl {"));
    }

    // =====================================================================
    //  Probedaten und Hilfen
    // =====================================================================

    /// <summary>Drei Kessel in den Textlängen der Testdatenbank (Bezeichner bis 76, Firmen bis 34 Zeichen).</summary>
    private static List<Katalogfilterzeile> Zeilen() => new()
    {
        Zeile(1, "Brennwertkessel Baureihe 2026 Plus mit modulierendem Brenner 3,8-24 kW Typ S",
              "Nordwerk Heiztechnik GmbH & Co. KG", "Holzpellets", 2412.5, 0.97, true),
        Zeile(2, "Kompaktgeraet 15", "Alpha", "Erdgas H", 15.0, 0.90, false),
        Zeile(3, "Niedertemperaturkessel Standard 40 kW", "Westhaus Waermetechnik AG", "Heizoel EL", 40.0, 0.98, false)
    };

    private static Katalogfilterzeile Zeile(int id, string name, string firma, string brennstoff,
                                            double pth, double eta, bool brennwert)
        => new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpHersteller, firma)
            .MitText(Katalogfilterprofil.SpBrennstoff, brennstoff)
            .MitZahl(Katalogfilterprofil.SpPtherm, pth)
            .MitZahl(Katalogfilterprofil.SpEta, eta, 3)
            .MitKennzeichen(Katalogfilterprofil.SpBrennwert, brennwert);

    private static Dictionary<string, int> Laengen()
        => Spaltenraenge.Laengen(Kessel(), Zeilen());

    /// <summary>
    /// Das Kesselprofil mit den Beschriftungen der Anwendung — die Breite eines Kopfes
    /// hängt an seinem TEXT, und ohne Übersetzer stünden die längeren Schlüssel darin.
    /// </summary>
    private static Katalogfilterprofil Kessel()
        => Katalogfilterprofil.Finde(Anlagenart.Heizkessel,
                                     s => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(s) ?? s);

    private IRenderedComponent<Katalogliste> Aufbauen(Katalogfilterstand? stand = null)
        => Render<Katalogliste>(p => p
            .Add(x => x.Profil, Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s))
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, stand ?? new Katalogfilterstand()));

    private static string Stilblatt()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));
    }

    private static string Stilblock(string selektor)
    {
        string css = Stilblatt();
        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}
