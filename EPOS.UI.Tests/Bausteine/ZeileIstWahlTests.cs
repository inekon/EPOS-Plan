using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Die Zeile ist die Wahl</b> — Konzept Administrationsdialoge, Stufe 2: V4 (Klick in
/// die Zeile wählt, keine Wahlspalte, 46 statt 53 px), V11 (↑ ↓ Pos1 Ende, ein
/// Tabulatorhalt, Enter unbelegt) und V10 (das Schloss eines Auslieferungssatzes).
///
/// <para>Geprüft am Baustein <see cref="Katalogliste"/> mit dem Schalter
/// <see cref="Katalogliste.ZeileIstWahl"/> und am <see cref="Raster{TZeile}"/>, das
/// Zeilenklasse und Tastenführung durchreicht. Ohne den Schalter bleibt alles, wie es war
/// — der Rückweg der Projektdialoge und Importe ist hier ebenso Gegenstand.</para>
///
/// <para><b>bunit misst keine Höhe</b> (Hausregel): Das Zeilenmaß wird als ANSAGE
/// (<c>ItemSize</c>) und als REGEL im Stilblatt geprüft; ob die Zeile im Browser wirklich
/// 46 px hoch ist, messen Katalog- und Rasterprobe.</para>
/// </summary>
public class ZeileIstWahlTests : EposBunitContext
{
    public ZeileIstWahlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static Katalogfilterprofil Profil() =>
        Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s);

    private static List<Katalogfilterzeile> Zeilen() => new()
    {
        new Katalogfilterzeile(1, "Alpha") { Geschuetzt = true }
            .MitText(Katalogfilterprofil.SpBezeichner, "Alpha")
            .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
            .MitZahl(Katalogfilterprofil.SpPtherm, 15.0),
        new Katalogfilterzeile(2, "Beta")
            .MitText(Katalogfilterprofil.SpBezeichner, "Beta")
            .MitText(Katalogfilterprofil.SpHersteller, "Buderus")
            .MitZahl(Katalogfilterprofil.SpPtherm, 80.0),
        new Katalogfilterzeile(3, "Gamma")
            .MitText(Katalogfilterprofil.SpBezeichner, "Gamma")
            .MitText(Katalogfilterprofil.SpHersteller, "Vaillant")
            .MitZahl(Katalogfilterprofil.SpPtherm, 40.0)
    };

    /// <summary>
    /// Ein Wirt, der die Wahl festhält — wie die Verwaltungen: Was die Liste meldet,
    /// kommt beim nächsten Zeichnen als <c>Gewaehlt</c> zurück.
    /// </summary>
    private IRenderedComponent<Katalogliste> Aufbauen(bool zeileIstWahl, List<string>? gemeldet = null,
                                                      string gewaehlt = "Alpha",
                                                      Katalogfilterstand? stand = null)
    {
        IRenderedComponent<Katalogliste>? cut = null;
        cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, stand ?? new Katalogfilterstand())
            .Add(x => x.Gewaehlt, gewaehlt)
            .Add(x => x.ZeileIstWahl, zeileIstWahl)
            .Add(x => x.GewaehltChanged, EventCallback.Factory.Create<string>(this, w => gemeldet?.Add(w))));
        return cut;
    }

    private static void Taste(IRenderedComponent<Katalogliste> cut, string taste)
        => cut.Find(".epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = taste });

    // =====================================================================
    //  Der Rückweg: ohne den Schalter bleibt die Liste, wie sie war
    // =====================================================================

    /// <summary>
    /// <b>Ohne <c>ZeileIstWahl</c></b> (Projektdialoge, Importe, Gesetze): Wahlspalte mit
    /// rundem Knopf, 53 px, kein Tabulatorhalt an der Hülle, keine Zeilenklasse.
    /// </summary>
    [Fact]
    public void Ohne_den_Schalter_bleibt_die_Wahlspalte_und_das_Mass_53()
    {
        var cut = Aufbauen(zeileIstWahl: false);

        Assert.Equal(3, cut.FindAll(".epos-anlagenwahl").Count);
        Assert.Empty(cut.FindAll(".epos-zeilenzelle"));
        Assert.Equal(53f, cut.Instance.Zeilenhoehe);
        Assert.Equal(53f, cut.FindComponent<Raster<Katalogfilterzeile>>().Instance.Zeilenhoehe);
        Assert.Null(cut.Find(".epos-raster-huelle").GetAttribute("tabindex"));
        Assert.DoesNotContain("epos-katalogliste--zeilenwahl", cut.Find(".epos-katalogliste").ClassName);
        Assert.All(cut.FindAll("tbody tr"), tr => Assert.True(string.IsNullOrEmpty(tr.ClassName)));
    }

    // =====================================================================
    //  V4 - die Zeile ist die Wahl
    // =====================================================================

    /// <summary>
    /// <b>Mit dem Schalter</b>: keine Wahlspalte, jede Zelle trägt die Klickfläche, das
    /// Maß ist 46 px — dieselbe Zahl als <c>ItemSize</c> und <c>--epos-rasterzeile</c>.
    /// </summary>
    [Fact]
    public void Mit_dem_Schalter_entfaellt_die_Wahlspalte_und_die_Zeile_ist_46_px()
    {
        var cut = Aufbauen(zeileIstWahl: true);

        Assert.Empty(cut.FindAll(".epos-anlagenwahl"));
        Assert.Empty(cut.FindAll("th.epos-spalte-wahl"));
        Assert.Equal(Profil().Spalten.Count, cut.FindAll("thead th").Count);
        Assert.Contains("epos-katalogliste--zeilenwahl", cut.Find(".epos-katalogliste").ClassName);

        Assert.Equal(Katalogliste.ZEILENHOEHE_ZEILENWAHL, cut.Instance.Zeilenhoehe);
        Assert.Equal(46f, cut.FindComponent<Raster<Katalogfilterzeile>>().Instance.Zeilenhoehe);

        // Jede Zelle jeder Zeile ist Klickfläche.
        foreach (var tr in cut.FindAll("tbody tr"))
            Assert.Equal(tr.QuerySelectorAll("td").Length, tr.QuerySelectorAll(".epos-zeilenzelle").Length);
    }

    /// <summary>
    /// <b>Ein Klick irgendwo in der Zeile wählt sie</b> — auch in einer Zahlenzelle.
    /// </summary>
    [Fact]
    public void Ein_Klick_in_die_Zeile_waehlt_sie()
    {
        var gemeldet = new List<string>();
        var cut = Aufbauen(zeileIstWahl: true, gemeldet: gemeldet);

        // Die letzte Zelle der dritten Zeile - die Zahl, nicht der Name.
        cut.FindAll("tbody tr")[2].QuerySelectorAll(".epos-zeilenzelle").Last().Click();

        Assert.Equal(new[] { "Gamma" }, gemeldet);
    }

    /// <summary>
    /// <b>Die Fokuszeile ist sichtbar</b>: Klasse <c>epos-zeile--gewaehlt</c> am
    /// <c>tr</c> (Fläche und linker Balken im Stilblatt) und <c>aria-current</c> an ihren
    /// Zellen — nur an dieser einen Zeile.
    /// </summary>
    [Fact]
    public void Die_Fokuszeile_ist_markiert_und_traegt_aria_current()
    {
        var cut = Aufbauen(zeileIstWahl: true, gewaehlt: "Beta");

        var zeilen = cut.FindAll("tbody tr");
        Assert.Equal("epos-zeile--gewaehlt", zeilen[1].ClassName);
        Assert.True(string.IsNullOrEmpty(zeilen[0].ClassName));
        Assert.True(string.IsNullOrEmpty(zeilen[2].ClassName));

        Assert.All(zeilen[1].QuerySelectorAll(".epos-zeilenzelle"),
                   z => Assert.Equal("true", z.GetAttribute("aria-current")));
        Assert.Empty(zeilen[0].QuerySelectorAll("[aria-current]"));
    }

    /// <summary>
    /// <b>Strg-Klick markiert für den Vergleich</b> und lässt die Wahl in Ruhe —
    /// dieselbe Regel wie am Wahlknopf (S3.3); die markierte Zeile trägt ihre Klasse.
    /// </summary>
    [Fact]
    public void Strg_Klick_markiert_fuer_den_Vergleich_statt_zu_waehlen()
    {
        var gemeldet = new List<string>();
        var cut = Aufbauen(zeileIstWahl: true, gemeldet: gemeldet);

        cut.FindAll("tbody tr")[1].QuerySelector(".epos-zeilenzelle")!.Click(new MouseEventArgs { CtrlKey = true });
        cut.FindAll("tbody tr")[2].QuerySelector(".epos-zeilenzelle")!.Click(new MouseEventArgs { CtrlKey = true });

        Assert.Empty(gemeldet);
        Assert.Equal(new[] { "Beta", "Gamma" }, cut.Instance.Markiert);
        Assert.Contains("epos-zeile--markiert", cut.FindAll("tbody tr")[1].ClassName);
        Assert.False(cut.Find(".epos-katalog-vergleichknopf").HasAttribute("disabled"));
    }

    // =====================================================================
    //  V11 - die Tastatur
    // =====================================================================

    /// <summary>
    /// <b>Die Liste ist EIN Tabulatorhalt</b> mit einer Beschriftung, die die Tasten
    /// nennt — man sieht ihnen nicht an, dass es sie gibt.
    /// </summary>
    [Fact]
    public void Die_Liste_ist_ein_Tabulatorhalt_mit_Beschriftung()
    {
        var cut = Aufbauen(zeileIstWahl: true);

        var huelle = cut.Find(".epos-raster-huelle");
        Assert.Equal("0", huelle.GetAttribute("tabindex"));
        Assert.Contains("Pos1", huelle.GetAttribute("aria-label") ?? "");
    }

    /// <summary>
    /// <b>↑ ↓ Pos1 Ende</b> bewegen die Wahl; am Rand bleibt sie stehen und meldet nichts
    /// doppelt.
    /// </summary>
    [Fact]
    public void Pfeiltasten_Pos1_und_Ende_bewegen_die_Wahl()
    {
        var gemeldet = new List<string>();
        var cut = Aufbauen(zeileIstWahl: true, gemeldet: gemeldet);

        Taste(cut, "ArrowDown");      // Alpha -> Beta
        Taste(cut, "End");            // -> Gamma
        Taste(cut, "ArrowDown");      // am Ende: nichts
        Taste(cut, "ArrowUp");        // -> Beta
        Taste(cut, "Home");           // -> Alpha
        Taste(cut, "ArrowUp");        // am Anfang: nichts

        Assert.Equal(new[] { "Beta", "Gamma", "Beta", "Alpha" }, gemeldet);
    }

    /// <summary>
    /// <b>Enter und die übrigen Tasten tun nichts</b> (Hausregel: in den Verwaltungen
    /// schreiben Knöpfe sofort) — auch nicht die Leertaste, die erst mit dem Kästchen
    /// der Mehrfachwahl (Stufe 3) eine Bedeutung bekommt.
    /// </summary>
    [Fact]
    public void Enter_und_Leertaste_tun_nichts()
    {
        var gemeldet = new List<string>();
        var cut = Aufbauen(zeileIstWahl: true, gemeldet: gemeldet);

        Taste(cut, "Enter");
        Taste(cut, " ");
        Taste(cut, "PageDown");

        Assert.Empty(gemeldet);
    }

    /// <summary>
    /// Blendet ein Filter die Wahl aus, beginnen ↓ und ↑ bei der ersten SICHTBAREN Zeile
    /// — die Tasten laufen über die gefilterte Liste, nicht über den ganzen Katalog.
    /// </summary>
    [Fact]
    public void Die_Tasten_laufen_ueber_die_gefilterte_Liste()
    {
        var gemeldet = new List<string>();
        var stand = new Katalogfilterstand { Suche = "Vaillant" };
        var cut = Aufbauen(zeileIstWahl: true, gemeldet: gemeldet, gewaehlt: "Beta", stand: stand);

        Assert.Equal(2, cut.FindAll("tbody tr").Count);   // Alpha, Gamma
        Taste(cut, "ArrowDown");
        Taste(cut, "End");

        Assert.Equal(new[] { "Alpha", "Gamma" }, gemeldet);
    }

    /// <summary>
    /// <b>Das Skript rollt die Wahl ins Bild</b>: Nach dem ersten Zeichnen hängt die Liste
    /// ihr Modul an die Hülle, nach einem Tastenschritt ruft sie <c>zeileZeigen</c> mit der
    /// Stelle der gewählten Zeile und dem Zeilenmaß 46.
    /// </summary>
    [Fact]
    public void Nach_einem_Tastenschritt_rollt_das_Skript_die_Wahl_ins_Bild()
    {
        // Lose bleibt der Rest (QuickGrid laedt sein eigenes Modul); das Modul der
        // Liste ist ausdruecklich eingerichtet und zeichnet seine Aufrufe auf.
        var modul = JSInterop.SetupModule(Katalogliste.MODUL);
        modul.SetupVoid("anmelden", _ => true);
        modul.SetupVoid("zeileZeigen", _ => true);

        IRenderedComponent<Katalogliste>? cut = null;
        string gewaehlt = "Alpha";
        cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.Gewaehlt, gewaehlt)
            .Add(x => x.ZeileIstWahl, true)
            .Add(x => x.GewaehltChanged, EventCallback.Factory.Create<string>(this, w => gewaehlt = w)));

        cut.WaitForAssertion(() => Assert.Single(modul.Invocations, i => i.Identifier == "anmelden"));

        Taste(cut, "End");

        cut.WaitForAssertion(() =>
        {
            var zeigen = modul.Invocations.Where(i => i.Identifier == "zeileZeigen").ToList();
            Assert.Single(zeigen);
            Assert.Equal(2, zeigen[0].Arguments[1]);
            Assert.Equal(46f, zeigen[0].Arguments[2]);
        });
        Assert.Equal("Gamma", gewaehlt);
    }

    /// <summary>
    /// <b>Ein neuer Zeigeanlass rollt die Fokuszeile ins Bild</b> (nach einer Übernahme aus
    /// dem Import, <c>Zeilenauswahl.Uebernahmen</c>) — über denselben Aufruf
    /// <c>zeileZeigen</c> wie ein Tastenschritt, mit der Stelle der Fokuszeile und dem
    /// Zeilenmaß 46. Der erste Anlass rollt nicht, und ohne neuen Anlass rollt auch ein
    /// Wechsel der Wahl von außen nicht.
    /// </summary>
    [Fact]
    public void Ein_neuer_Zeigeanlass_rollt_die_Fokuszeile_ins_Bild()
    {
        var modul = JSInterop.SetupModule(Katalogliste.MODUL);
        modul.SetupVoid("anmelden", _ => true);
        modul.SetupVoid("zeileZeigen", _ => true);

        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.Gewaehlt, "Alpha")
            .Add(x => x.ZeileIstWahl, true)
            .Add(x => x.Zeigeanlass, 5));

        cut.WaitForAssertion(() => Assert.Single(modul.Invocations, i => i.Identifier == "anmelden"));

        // Derselbe Anlass, eine andere Wahl: kein Rollen.
        cut.Render(p => p.Add(x => x.Gewaehlt, "Beta").Add(x => x.Zeigeanlass, 5));
        Assert.DoesNotContain(modul.Invocations, i => i.Identifier == "zeileZeigen");

        // Ein NEUER Anlass: die Fokuszeile Gamma (Stelle 2) ins Bild.
        cut.Render(p => p.Add(x => x.Gewaehlt, "Gamma").Add(x => x.Zeigeanlass, 6));

        cut.WaitForAssertion(() =>
        {
            var zeigen = modul.Invocations.Where(i => i.Identifier == "zeileZeigen").ToList();
            Assert.Single(zeigen);
            Assert.Equal(2, zeigen[0].Arguments[1]);
            Assert.Equal(46f, zeigen[0].Arguments[2]);
        });
    }

    /// <summary>
    /// <b>Der linke Balken weicht nicht mit der ersten Spalte</b>: Führt das Profil vor
    /// dem Namen eine Spalte mit Rang (die Wärmepumpe: Hersteller), trägt die erste immer
    /// stehende Spalte die Stufe der ersten als <c>epos-balken-ab-N</c> — das Stilblatt
    /// zeichnet den Balken dort genau unterhalb von N. Im Heizkessel (Name zuerst) gibt es
    /// keinen Ersatz.
    /// </summary>
    [Fact]
    public void Weicht_die_erste_Spalte_uebernimmt_der_Name_den_Balken()
    {
        var wp = Katalogfilterprofil.Finde(Anlagenart.Waermepumpe, s => s);
        var zeilen = new List<Katalogfilterzeile>
        {
            new Katalogfilterzeile(1, "WP 1")
                .MitText(Katalogfilterprofil.SpHersteller, "Nordwerk Heiztechnik GmbH")
                .MitText(Katalogfilterprofil.SpBezeichner, "WP 1")
                .MitZahl(Katalogfilterprofil.SpNennleistung, 12.0)
        };
        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, wp)
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.Gewaehlt, "WP 1")
            .Add(x => x.ZeileIstWahl, true));

        int stufe = cut.Instance.Stufen[Katalogfilterprofil.SpHersteller];
        Assert.True(stufe > 0);

        var zellen = cut.FindAll("tbody tr")[0].QuerySelectorAll("td");
        Assert.Contains("epos-spalte-ab-" + stufe, zellen[0].ClassName);            // Hersteller weicht
        Assert.Contains("epos-balken-ab-" + stufe, zellen[1].ClassName);            // Modell traegt den Balken
        Assert.DoesNotContain("epos-balken-ab-", zellen[0].ClassName);

        // Der Heizkessel beginnt mit dem Namen - kein Ersatz noetig.
        var kessel = Aufbauen(zeileIstWahl: true);
        Assert.DoesNotContain("epos-balken-ab-", kessel.Markup);
    }

    /// <summary>
    /// In der MEHRFACHWAHL wirkt der Schalter nicht: Das Kästchen bleibt eigene Spalte
    /// (V6), das Maß 53 px.
    /// </summary>
    [Fact]
    public void In_der_Mehrfachwahl_wirkt_der_Schalter_nicht()
    {
        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.Mehrfach, true)
            .Add(x => x.IstGewaehlt, _ => false)
            .Add(x => x.ZeileIstWahl, true));

        Assert.Equal(3, cut.FindAll(".epos-anlagenwahl").Count);
        Assert.Empty(cut.FindAll(".epos-zeilenzelle"));
        Assert.Equal(53f, cut.Instance.Zeilenhoehe);
    }

    // =====================================================================
    //  V10 - das Schloss
    // =====================================================================

    /// <summary>
    /// <b>Nur das Schloss</b> (AD-Q13): Der Auslieferungssatz trägt es hinter dem Namen,
    /// ohne Wort — der Satz steht im Kurztext und für die Sprachausgabe im
    /// <c>aria-label</c>. Der Textinhalt der Zelle bleibt der Name. In BEIDEN
    /// Betriebsarten, weil die Profile ihre Spalten „Auslieferung" und „Schreibschutz"
    /// dafür abgegeben haben.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ein_Auslieferungssatz_traegt_das_Schloss_hinter_dem_Namen(bool zeileIstWahl)
    {
        var cut = Aufbauen(zeileIstWahl: zeileIstWahl);

        var zeilen = cut.FindAll("tbody tr");
        var schloss = zeilen[0].QuerySelector(".epos-schloss");
        Assert.NotNull(schloss);
        Assert.Equal("img", schloss!.GetAttribute("role"));
        Assert.Equal("Auslieferungssatz – nur lesen", schloss.GetAttribute("title"));
        Assert.Equal(schloss.GetAttribute("title"), schloss.GetAttribute("aria-label"));
        Assert.Equal("", schloss.TextContent.Trim());
        Assert.Null(zeilen[1].QuerySelector(".epos-schloss"));

        // Der Name steht als Text in derselben Zelle - das Schloss nimmt ihm nichts.
        Assert.Equal("Alpha", schloss.ParentElement!.TextContent.Trim());
    }

    /// <summary>Der Kurztext kommt aus dem Bündel — die Verwaltung mit Duplizieren setzt ihren.</summary>
    [Fact]
    public void Der_Kurztext_des_Schlosses_kommt_aus_dem_Buendel()
    {
        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.ZeileIstWahl, true)
            .Add(x => x.Texte, new Katalogfiltertexte
            {
                Schloss = WindowsFormsApplication1.MyResource.Resource.ADM_SCHLOSS_DUPLIZIEREN
            }));

        Assert.Equal("Auslieferungssatz – nur lesen, Duplizieren oder Schloss aufheben erlaubt",
                     cut.Find(".epos-schloss").GetAttribute("title"));
    }

    // =====================================================================
    //  Raster - Zeilenklasse und Tastenführung durchgereicht
    // =====================================================================

    private sealed record Posten(string Name);

    private static RenderFragment Namensspalte() => bau =>
    {
        bau.OpenComponent<Microsoft.AspNetCore.Components.QuickGrid.PropertyColumn<Posten, string>>(0);
        bau.AddComponentParameter(1, "Property",
                                  (System.Linq.Expressions.Expression<Func<Posten, string>>)(z => z.Name));
        bau.AddComponentParameter(2, "Title", "Name");
        bau.CloseComponent();
    };

    /// <summary>
    /// <b>Raster reicht beides durch</b>: die Zeilenklasse an QuickGrids <c>RowClass</c>,
    /// die Tastenführung als Tabulatorhalt mit Beschriftung. Ohne Delegat bleibt die Hülle
    /// ohne <c>tabindex</c> — die übrigen Listen des Hauses bleiben, wie sie sind.
    /// </summary>
    [Fact]
    public void Raster_reicht_Zeilenklasse_und_Tastenfuehrung_durch()
    {
        var posten = new[] { new Posten("A"), new Posten("B") }.AsQueryable();
        string? taste = null;

        var mit = Render<Raster<Posten>>(p => p
            .Add(x => x.Zeilen, posten)
            .Add(x => x.Zeilenklasse, z => z.Name == "B" ? "epos-zeile--gewaehlt" : null)
            .Add(x => x.Tastenfuehrung, EventCallback.Factory.Create<KeyboardEventArgs>(this, e => taste = e.Key))
            .Add(x => x.Tastenbeschriftung, "Liste")
            .Add(x => x.KindInhalt, Namensspalte()));

        var huelle = mit.Find("div.epos-raster-huelle");
        Assert.Equal("0", huelle.GetAttribute("tabindex"));
        Assert.Equal("Liste", huelle.GetAttribute("aria-label"));
        Assert.Equal("epos-zeile--gewaehlt", mit.FindAll("tbody tr")[1].ClassName);

        huelle.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("ArrowDown", taste);

        var ohne = Render<Raster<Posten>>(p => p
            .Add(x => x.Zeilen, posten)
            .Add(x => x.KindInhalt, Namensspalte()));
        Assert.Null(ohne.Find("div.epos-raster-huelle").GetAttribute("tabindex"));
        Assert.Null(ohne.Find("div.epos-raster-huelle").GetAttribute("aria-label"));
    }

    // =====================================================================
    //  Das Stilblatt - das Maß als REGEL
    // =====================================================================

    /// <summary>
    /// <b>Das Maß steht als Regel im Stilblatt</b>: Die Zelle gibt ihre Polsterung ab
    /// (gewichtiger als die Hausregel), die Klickfläche ist 45 px hoch und bricht nicht um
    /// — mit der 1-px-Linie sind das genau <see cref="Katalogliste.ZEILENHOEHE_ZEILENWAHL"/>.
    /// </summary>
    [Fact]
    public void Das_Stilblatt_setzt_die_Klickflaeche_auf_das_Zeilenmass()
    {
        string blatt = Stilblatt().Replace("\r\n", "\n");

        Assert.Contains(".epos-katalogliste--zeilenwahl table.epos-raster.quickgrid > tbody > tr > td {\n    padding: 0;\n}", blatt);

        int a = blatt.IndexOf(".epos-zeilenzelle {", StringComparison.Ordinal);
        Assert.True(a > 0, "Regel .epos-zeilenzelle fehlt");
        string regel = blatt.Substring(a, blatt.IndexOf('}', a) - a);
        Assert.Contains("height: 45px;", regel);
        Assert.Contains("white-space: nowrap;", regel);
        Assert.Contains("box-sizing: border-box;", regel);
        Assert.Equal(45f + 1f, Katalogliste.ZEILENHOEHE_ZEILENWAHL);

        // Die Fokuszeile und das Schloss haben auch im Hochkontrastbetrieb eine Form.
        Assert.Contains("tr.epos-zeile--gewaehlt > td {\n        outline: 2px solid Highlight;", blatt);
        Assert.Contains(".epos-schloss {\n        color: CanvasText;", blatt);
    }

    private static string Stilblatt()
    {
        var d = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
        {
            string kandidat = System.IO.Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css");
            if (System.IO.File.Exists(kandidat)) return System.IO.File.ReadAllText(kandidat);
        }
        Assert.Fail("epos-ui.css wurde nicht gefunden.");
        return "";
    }
}
