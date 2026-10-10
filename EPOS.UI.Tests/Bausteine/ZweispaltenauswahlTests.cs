using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Die Katalogauswahl V1 „Gerahmt und gestapelt"</b> (Konzept Projektdialoge mit
/// Katalogauswahl, Entscheide KA‑E‑1 bis KA‑E‑11, Zielbild 4.1 bis 4.8) im Baustein
/// <see cref="Zweispaltenauswahl"/>: drei gerahmte Bereiche mit Kopfleiste und
/// Kennfarbe, die Trennlinie mit Grenzen und Merken, die Detailzeile, die Mehrfachwahl
/// samt Kopfhäkchen auf der gefilterten Liste, Enter und Doppelklick.
///
/// <para>Geprüft wird dreierlei: der BAUSTEIN, die REGEL im Stilblatt (eine bunit-Probe
/// sieht sie nicht — Lehre W6-B-1; Lage und Rollbereiche misst
/// <c>Proben/Rasterprobe/rollbereichprobe.mjs</c>) und der BESTAND (alle zwölf Wirte
/// nehmen den Baustein).</para>
/// </summary>
public class ZweispaltenauswahlTests : EposBunitContext
{
    private static RenderFragment Absatz(string klasse, string text) => b =>
    {
        b.OpenElement(0, "p");
        b.AddAttribute(1, "class", klasse);
        b.AddContent(2, text);
        b.CloseElement();
    };

    /// <summary>Die kleinste tragfähige Probe: zwei Bereiche, zwei Knöpfe, ein Satz.</summary>
    private IRenderedComponent<Zweispaltenauswahl> Aufbauen(
        bool nurRechts = false,
        bool uebernehmenGesperrt = false,
        bool entfernenGesperrt = false,
        Action? uebernommen = null,
        Action? entfernt = null,
        string dialogname = "",
        bool mitSatz = true,
        Action<ComponentParameterCollectionBuilder<Zweispaltenauswahl>>? mehr = null)
        => Render<Zweispaltenauswahl>(p =>
        {
            p.Add(x => x.Dialogname, dialogname)
             .Add(x => x.LinksTitel, "ausgewählte Gebäude im Projekt:")
             .Add(x => x.RechtsTitel, "Gebäude in DB:")
             .Add(x => x.NurRechts, nurRechts)
             .Add(x => x.UebernehmenGesperrt, uebernehmenGesperrt)
             .Add(x => x.EntfernenGesperrt, entfernenGesperrt)
             .Add(x => x.Uebernehmen, () => uebernommen?.Invoke())
             .Add(x => x.Entfernen, () => entfernt?.Invoke())
             .Add(x => x.Links, Absatz("probe-links", "Projektliste"))
             .Add(x => x.Rechts, Absatz("probe-rechts", "Katalogliste"));
            if (mitSatz) p.Add(x => x.Satz, Absatz("probe-satz", "Alle Daten"));
            mehr?.Invoke(p);
        });

    /// <summary>Setzt eine flüchtige Ablage für <c>WindowsFormsApplication1.Dienste.Einstellungen</c> und stellt sie zurück.</summary>
    private static void MitAblage(Action<IEinstellungen> lauf)
    {
        IEinstellungen alt = WindowsFormsApplication1.Dienste.Einstellungen;
        var ablage = new FluechtigeEinstellungen();
        WindowsFormsApplication1.Dienste.Einstellungen = ablage;
        try { lauf(ablage); }
        finally { WindowsFormsApplication1.Dienste.Einstellungen = alt; }
    }

    private static string Stil(IRenderedComponent<Zweispaltenauswahl> cut)
        => cut.Find(".epos-zweispalten").GetAttribute("style") ?? "";

    // =====================================================================
    // Aufbau: drei gerahmte Bereiche (4.1)
    // =====================================================================

    /// <summary>
    /// Projekt — Trennlinie — Katalog — Satz, in dieser Reihenfolge im Markup; sie ist
    /// zugleich der Tabulatorweg (4.7).
    /// </summary>
    [Fact]
    public void Projekt_Trennlinie_Katalog_Satz_stehen_in_dieser_Reihenfolge()
    {
        var cut = Aufbauen();

        var bereiche = cut.FindAll(".epos-zweispalten > *").Select(e => e.ClassName ?? "").ToList();

        Assert.Equal(4, bereiche.Count);
        Assert.Contains("epos-zweispalten-bereich--projekt", bereiche[0]);
        Assert.Contains("epos-zweispalten-trenner", bereiche[1]);
        Assert.Contains("epos-zweispalten-bereich--katalog", bereiche[2]);
        Assert.Contains("epos-zweispalten-bereich--satz", bereiche[3]);

        Assert.Single(cut.FindAll(".epos-zweispalten-bereich--projekt .epos-zweispalten-inhalt > .probe-links"));
        Assert.Single(cut.FindAll(".epos-zweispalten-bereich--katalog .epos-zweispalten-inhalt > .probe-rechts"));
        Assert.Single(cut.FindAll(".epos-zweispalten-bereich--satz .epos-zweispalten-satz > .probe-satz"));
    }

    /// <summary>Jeder Bereich trägt Marke und Überschrift in seiner Kopfleiste und ein aria-label.</summary>
    [Fact]
    public void Jeder_Bereich_traegt_Kopfleiste_Marke_und_aria_Beschriftung()
    {
        var cut = Aufbauen();

        IElement projekt = cut.Find(".epos-zweispalten-bereich--projekt");
        IElement katalog = cut.Find(".epos-zweispalten-bereich--katalog");

        Assert.Equal("group", projekt.GetAttribute("role"));
        Assert.Equal("ausgewählte Gebäude im Projekt:", projekt.GetAttribute("aria-label"));
        Assert.Equal("Gebäude in DB:", katalog.GetAttribute("aria-label"));

        Assert.Equal("ausgewählte Gebäude im Projekt:",
                     projekt.QuerySelector(".epos-zweispalten-kopfleiste > h2.epos-untergruppe")!.TextContent);
        Assert.Equal("Gebäude in DB:",
                     katalog.QuerySelector(".epos-zweispalten-kopfleiste > h2.epos-untergruppe")!.TextContent);

        Assert.Equal(Resource.AUSWAHL_MARKE_PROJEKT,
                     projekt.QuerySelector(".epos-zweispalten-marke--projekt")!.TextContent);
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOG,
                     katalog.QuerySelector(".epos-zweispalten-marke--katalog")!.TextContent);
        Assert.Equal(Resource.AUSWAHL_MARKE_SATZ,
                     cut.Find(".epos-zweispalten-bereich--satz .epos-zweispalten-marke--satz").TextContent);
    }

    /// <summary>
    /// Jeder Knopf steht in der Kopfleiste des Bereichs, auf den er wirkt (KA‑E‑1):
    /// „Entfernen" beim Projekt, „Übernehmen" beim Katalog — Zeichen plus Klartext,
    /// das Zeichen sprachausgabe-blind.
    /// </summary>
    [Fact]
    public void Jeder_Richtungsknopf_steht_in_der_Kopfleiste_seines_Bereichs()
    {
        var cut = Aufbauen();

        IElement weg = cut.Find(".epos-zweispalten-bereich--projekt > .epos-zweispalten-kopfleiste > .epos-zweispalten-knopf--entfernen");
        IElement hin = cut.Find(".epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste > .epos-zweispalten-knopf--uebernehmen");

        Assert.Equal(2, cut.FindAll(".epos-zweispalten-knopf--richtung").Count);
        Assert.Equal("▲", hin.QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("▼", weg.QuerySelector(".epos-zweispalten-pfeil")!.TextContent);
        Assert.Equal("In das Projekt übernehmen", hin.QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Equal("Aus dem Projekt entfernen", weg.QuerySelector(".epos-zweispalten-knopftext")!.TextContent);
        Assert.Contains("Datenbankliste", hin.GetAttribute("title") ?? "");
        Assert.Contains("Projektliste", weg.GetAttribute("title") ?? "");
        foreach (IElement pfeil in cut.FindAll(".epos-zweispalten-pfeil"))
            Assert.Equal("true", pfeil.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Ohne_Markierung_ist_der_jeweilige_Knopf_gesperrt()
    {
        var cut = Aufbauen(uebernehmenGesperrt: true);
        Assert.True(cut.Find(".epos-zweispalten-knopf--uebernehmen").HasAttribute("disabled"));
        Assert.False(cut.Find(".epos-zweispalten-knopf--entfernen").HasAttribute("disabled"));

        cut = Aufbauen(entfernenGesperrt: true);
        Assert.False(cut.Find(".epos-zweispalten-knopf--uebernehmen").HasAttribute("disabled"));
        Assert.True(cut.Find(".epos-zweispalten-knopf--entfernen").HasAttribute("disabled"));
    }

    [Fact]
    public void Jeder_Knopf_meldet_seinen_Klick()
    {
        int hin = 0, weg = 0;
        var cut = Aufbauen(uebernommen: () => hin++, entfernt: () => weg++);

        cut.Find(".epos-zweispalten-knopf--uebernehmen").Click();
        cut.Find(".epos-zweispalten-knopf--entfernen").Click();

        Assert.Equal(1, hin);
        Assert.Equal(1, weg);
    }

    /// <summary>Verwaltungsbetriebsart: Projektbereich und Trennlinie entfallen, der Katalog steht allein.</summary>
    [Fact]
    public void NurRechts_laesst_Projektbereich_Trennlinie_und_Richtungsknoepfe_weg()
    {
        var cut = Aufbauen(nurRechts: true);

        Assert.Empty(cut.FindAll(".epos-zweispalten-bereich--projekt"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-trenner"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-knopf--richtung"));
        Assert.Contains("epos-zweispalten--nurkatalog", cut.Find(".epos-zweispalten").ClassName);
        Assert.Single(cut.FindAll(".epos-zweispalten-bereich--katalog .probe-rechts"));
    }

    // =====================================================================
    // Trennlinie (4.3)
    // =====================================================================

    /// <summary>Die Trennlinie ist ein fokussierbarer waagrechter Separator mit Namen und Wert.</summary>
    [Fact]
    public void Die_Trennlinie_ist_ein_fokussierbarer_Separator_mit_Vorgabe()
    {
        var cut = Aufbauen();
        IElement t = cut.Find(".epos-zweispalten-trenner");

        Assert.Equal("separator", t.GetAttribute("role"));
        Assert.Equal("horizontal", t.GetAttribute("aria-orientation"));
        Assert.Equal("0", t.GetAttribute("tabindex"));
        Assert.Equal(Resource.AUSWAHL_TRENNER, t.GetAttribute("aria-label"));
        Assert.Equal(Zweispaltenauswahl.VORGABEHOEHE.ToString(CultureInfo.InvariantCulture), t.GetAttribute("aria-valuenow"));
        Assert.Contains($"--epos-trenner-hoehe: {Zweispaltenauswahl.VORGABEHOEHE}px", Stil(cut));
        Assert.Contains($"--epos-trenner-min: {Zweispaltenauswahl.MINDESTHOEHE}px", Stil(cut));
    }

    /// <summary>Vorgabe Kopf + zwei Zeilen, Untergrenze Kopf + eine Zeile, Katalog Kopf + zwei Zeilen.</summary>
    [Fact]
    public void Die_Grenzen_folgen_aus_Kopfzeile_und_Zeilenmass()
    {
        Assert.Equal(Zweispaltenauswahl.LISTENKOPF + Zweispaltenauswahl.ZEILE + 2, Zweispaltenauswahl.MINDESTHOEHE);
        Assert.Equal(Zweispaltenauswahl.MINDESTHOEHE + Zweispaltenauswahl.ZEILE, Zweispaltenauswahl.VORGABEHOEHE);
        Assert.Equal(Zweispaltenauswahl.KATALOG_LISTENKOPF + 2 * Zweispaltenauswahl.ZEILE + 2, Zweispaltenauswahl.KATALOG_MINDESTHOEHE);
        Assert.Equal((int)Raster<object>.ZEILENHOEHE, Zweispaltenauswahl.ZEILE);
    }

    /// <summary>Pfeil hoch/runter um eine Zeile, Pos1 an die Untergrenze; gemerkt je Dialog.</summary>
    [Fact]
    public void Pfeiltasten_und_Pos1_setzen_die_Hoehe_in_Grenzen_und_merken_sie()
    {
        MitAblage(ablage =>
        {
            var cut = Aufbauen(dialogname: "Heizkessel");
            IElement t = cut.Find(".epos-zweispalten-trenner");
            int v = Zweispaltenauswahl.VORGABEHOEHE, z = Zweispaltenauswahl.ZEILE;

            t.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
            Assert.Equal(v + z, cut.Instance.Trennerhoehe);
            Assert.Equal(v + z, ablage.LiesZahl("Katalogauswahl.Trenner.Heizkessel"));

            cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            Assert.Equal(Zweispaltenauswahl.MINDESTHOEHE, cut.Instance.Trennerhoehe);   // geklemmt

            cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });
            Assert.Equal(Zweispaltenauswahl.MINDESTHOEHE, cut.Instance.Trennerhoehe);   // bleibt an der Grenze

            cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "End" });
            Assert.Equal(Zweispaltenauswahl.OBERGRENZE_OHNE_MASS, cut.Instance.Trennerhoehe); // ohne Messung: Rastergrenze
            cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "Home" });
            Assert.Equal(Zweispaltenauswahl.MINDESTHOEHE, cut.Instance.Trennerhoehe);
            Assert.Contains($"--epos-trenner-hoehe: {Zweispaltenauswahl.MINDESTHOEHE}px", Stil(cut));

            // Andere Tasten tun nichts.
            cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "a" });
            Assert.Equal(Zweispaltenauswahl.MINDESTHOEHE, cut.Instance.Trennerhoehe);
        });
    }

    /// <summary>Mit Messung aus dem Skript gilt dessen Obergrenze (die Katalogliste behält Kopf und zwei Zeilen).</summary>
    [Fact]
    public void Mit_Messung_klemmt_Ende_an_die_gemessene_Obergrenze()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var modul = JSInterop.SetupModule(Zweispaltenauswahl.MODUL);
        modul.SetupVoid("anmelden", _ => true);
        modul.Setup<Zweispaltenauswahl.Trennermasse?>("masse", _ => true)
             .SetResult(new Zweispaltenauswahl.Trennermasse(139, 85, 300));

        var cut = Aufbauen();
        cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal(300, cut.Instance.Trennerhoehe);
        cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(139 + Zweispaltenauswahl.ZEILE, cut.Instance.Trennerhoehe);   // ein Schritt ab der gemessenen Höhe
        modul.VerifyInvoke("anmelden");
    }

    /// <summary>Die gemerkte Höhe kommt beim nächsten Öffnen wieder; Doppelklick stellt die Vorgabe her.</summary>
    [Fact]
    public void Gemerkte_Hoehe_gilt_beim_Oeffnen_Doppelklick_stellt_die_Vorgabe_her()
    {
        MitAblage(ablage =>
        {
            ablage.SchreibZahl("Katalogauswahl.Trenner.BHKW", 250);
            var cut = Aufbauen(dialogname: "BHKW");
            Assert.Equal(250, cut.Instance.Trennerhoehe);

            var anderer = Aufbauen(dialogname: "Heizkessel");
            Assert.Equal(Zweispaltenauswahl.VORGABEHOEHE, anderer.Instance.Trennerhoehe);  // je Dialog

            cut.Find(".epos-zweispalten-trenner").DoubleClick();
            Assert.Equal(Zweispaltenauswahl.VORGABEHOEHE, cut.Instance.Trennerhoehe);
            Assert.Equal(0, ablage.LiesZahl("Katalogauswahl.Trenner.BHKW", 0));
        });
    }

    /// <summary>Ohne Dialognamen wird nichts gemerkt — es gilt die Vorgabe.</summary>
    [Fact]
    public void Ohne_Dialognamen_wird_nichts_gemerkt()
    {
        MitAblage(ablage =>
        {
            var cut = Aufbauen(dialogname: "");
            cut.Find(".epos-zweispalten-trenner").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
            Assert.Equal(0, ablage.LiesZahl("Katalogauswahl.Trenner.", 0));
        });
    }

    /// <summary>Das Skript meldet die gezogene Höhe; der Baustein klemmt und merkt sie.</summary>
    [Fact]
    public void Gezogene_Hoehe_wird_geklemmt_und_gemerkt()
    {
        MitAblage(ablage =>
        {
            var cut = Aufbauen(dialogname: "Pufferspeicher");
            cut.InvokeAsync(() => cut.Instance.TrennerGezogen(10));
            Assert.Equal(Zweispaltenauswahl.MINDESTHOEHE, cut.Instance.Trennerhoehe);
            cut.InvokeAsync(() => cut.Instance.TrennerGezogen(321));
            Assert.Equal(321, ablage.LiesZahl("Katalogauswahl.Trenner.Pufferspeicher"));
            Assert.Contains("--epos-trenner-hoehe: 321px", Stil(cut));
        });
    }

    // =====================================================================
    // Detailzeile (4.4)
    // =====================================================================

    /// <summary>Zugeklappt beim Öffnen; der Inhalt bleibt im Baum (hidden), Klick klappt auf und zu.</summary>
    [Fact]
    public void Die_Detailzeile_startet_zugeklappt_und_klappt_auf_und_zu()
    {
        bool? gemeldet = null;
        var cut = Aufbauen(mehr: p => p.Add(x => x.SatzGeklappt, (bool b) => gemeldet = b)
                                       .Add(x => x.SatzArt, Satzmarke.Katalogsatz)
                                       .Add(x => x.SatzName, "Kessel 30 kW")
                                       .Add(x => x.SatzKenndaten, "30 kW · Erdgas"));

        IElement zeile = cut.Find(".epos-zweispalten-satzzeile");
        Assert.Equal("false", zeile.GetAttribute("aria-expanded"));
        Assert.True(cut.Find(".epos-zweispalten-satz").HasAttribute("hidden"));
        Assert.Single(cut.FindAll(".epos-zweispalten-satz .probe-satz"));     // im Baum
        Assert.Equal(Resource.AUSWAHL_MARKE_KATALOGSATZ, cut.Find(".epos-zweispalten-marke--satz").TextContent);
        Assert.Equal("Kessel 30 kW", cut.Find(".epos-zweispalten-satzname").TextContent);
        Assert.Equal("30 kW · Erdgas", cut.Find(".epos-zweispalten-satzkenndaten").TextContent);

        zeile.Click();
        Assert.Equal("true", cut.Find(".epos-zweispalten-satzzeile").GetAttribute("aria-expanded"));
        Assert.False(cut.Find(".epos-zweispalten-satz").HasAttribute("hidden"));
        Assert.Contains("epos-zweispalten--satz-offen", cut.Find(".epos-zweispalten").ClassName);
        Assert.True(gemeldet);

        cut.Find(".epos-zweispalten-satzzeile").Click();
        Assert.True(cut.Find(".epos-zweispalten-satz").HasAttribute("hidden"));
        Assert.False(gemeldet);
    }

    /// <summary>Ohne Satzname sagt die Zeile „Kein Satz gewählt"; ohne Satz fehlt die Zeile.</summary>
    [Fact]
    public void Ohne_Satz_fehlt_die_Detailzeile()
    {
        Assert.Equal(Resource.AUSWAHL_SATZ_LEER, Aufbauen().Find(".epos-zweispalten-satzname").TextContent);
        Assert.Empty(Aufbauen(mitSatz: false).FindAll(".epos-zweispalten-bereich--satz"));
    }

    /// <summary>Esc klappt die Detailzeile zu (das Skript ruft <c>SatzSchliessen</c>); zu bleibt zu.</summary>
    [Fact]
    public void Esc_klappt_zuerst_die_Detailzeile_zu()
    {
        var cut = Aufbauen();
        cut.Find(".epos-zweispalten-satzzeile").Click();
        cut.InvokeAsync(() => cut.Instance.SatzSchliessen());
        Assert.False(cut.Instance.SatzOffen);
        cut.InvokeAsync(() => cut.Instance.SatzSchliessen());
        Assert.False(cut.Instance.SatzOffen);
    }

    // =====================================================================
    // Tastatur und Doppelklick in den Listen (4.7, KA‑E‑6)
    // =====================================================================

    /// <summary>Enter und Doppelklick auf einer Katalogzeile übernehmen — nicht, wenn gesperrt.</summary>
    [Fact]
    public void Enter_und_Doppelklick_im_Katalog_uebernehmen()
    {
        int hin = 0;
        var cut = Aufbauen(uebernommen: () => hin++);
        cut.InvokeAsync(() => cut.Instance.ListenTaste("Katalog", "Enter"));
        Assert.Equal(1, hin);

        var gesperrt = Aufbauen(uebernehmenGesperrt: true, uebernommen: () => hin++);
        gesperrt.InvokeAsync(() => gesperrt.Instance.ListenTaste("Katalog", "Enter"));
        Assert.Equal(1, hin);

        var nurKatalog = Aufbauen(nurRechts: true, uebernommen: () => hin++);
        nurKatalog.InvokeAsync(() => nurKatalog.Instance.ListenTaste("Katalog", "Enter"));
        Assert.Equal(1, hin);   // ohne Projekt kein Übernehmen
    }

    /// <summary>Enter in der Projektliste klappt die Detailzeile auf, Entf entfernt.</summary>
    [Fact]
    public void Enter_und_Entf_in_der_Projektliste()
    {
        int weg = 0;
        var cut = Aufbauen(entfernt: () => weg++);
        cut.InvokeAsync(() => cut.Instance.ListenTaste("Projekt", "Enter"));
        Assert.True(cut.Instance.SatzOffen);
        cut.InvokeAsync(() => cut.Instance.ListenTaste("Projekt", "Entf"));
        Assert.Equal(1, weg);
        cut.InvokeAsync(() => cut.Instance.ListenTaste("Katalog", "Entf"));
        Assert.Equal(1, weg);   // Entf wirkt nur im Projekt
    }

    /// <summary>Das Skript wird geladen und an der Wurzel angemeldet.</summary>
    [Fact]
    public void Das_Skript_wird_an_der_Wurzel_angemeldet()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var modul = JSInterop.SetupModule(Zweispaltenauswahl.MODUL);
        Aufbauen();
        modul.VerifyInvoke("anmelden");
        Assert.True(File.Exists(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-zweispalten.js")));
        string skript = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-zweispalten.js"));
        foreach (string aufruf in new[] { "\"TrennerGezogen\"", "\"ListenTaste\"", "\"SatzSchliessen\"", "dblclick", "Enter", "Delete" })
            Assert.Contains(aufruf, skript);
    }

    // =====================================================================
    // Mehrfachwahl (4.5, KA‑E‑5)
    // =====================================================================

    private static RenderFragment Kaestchenliste(Auswahlbereich bereich, IEnumerable<string> schluessel) => b =>
    {
        b.OpenComponent<Wahlkaestchen>(0);
        b.AddAttribute(1, nameof(Wahlkaestchen.Bereich), bereich);
        b.CloseComponent();
        foreach (string s in schluessel)
        {
            b.OpenComponent<Wahlkaestchen>(2);
            b.SetKey(s);
            b.AddAttribute(3, nameof(Wahlkaestchen.Bereich), bereich);
            b.AddAttribute(4, nameof(Wahlkaestchen.Schluessel), s);
            b.CloseComponent();
        }
    };

    /// <summary>
    /// Das Kopfhäkchen wählt alle Zeilen der GEFILTERTEN Liste; eine verborgene Wahl bleibt
    /// und wird genannt („3 gewählt, 1 verborgen").
    /// </summary>
    [Fact]
    public void Das_Kopfhaekchen_waehlt_die_gefilterte_Liste_und_die_Kopfleiste_nennt_Verborgene()
    {
        using var _ = new Kulturvorrichtung("de-DE");
        var wahl = new Bereichswahl();
        wahl.Umschalten("D");                                   // gewählt, später verborgen
        var sichtbar = new[] { "A", "B", "C" };
        int gemeldet = 0;

        var cut = Aufbauen(mehr: p => p
            .Add(x => x.KatalogWahl, wahl)
            .Add(x => x.KatalogSichtbar, sichtbar)
            .Add(x => x.WahlGeaendert, (Auswahlbereich _) => gemeldet++)
            .Add(x => x.Rechts, Kaestchenliste(Auswahlbereich.Katalog, sichtbar)));

        IElement kopf = cut.Find(".epos-wahlkaestchen--kopf");
        Assert.Equal("false", kopf.GetAttribute("aria-checked"));
        Assert.Equal("1 gewählt, 1 verborgen", cut.Find(".epos-zweispalten-bereich--katalog .epos-zweispalten-wahlzahl").TextContent);

        kopf.Click();
        Assert.Equal(new[] { "D", "A", "B", "C" }, wahl.Gewaehlte);
        Assert.Equal("true", cut.Find(".epos-wahlkaestchen--kopf").GetAttribute("aria-checked"));
        Assert.Equal("4 gewählt, 1 verborgen", cut.Find(".epos-zweispalten-bereich--katalog .epos-zweispalten-wahlzahl").TextContent);
        Assert.Equal(1, gemeldet);

        cut.FindAll(".epos-wahlkaestchen:not(.epos-wahlkaestchen--kopf)")[1].Click();   // B ab
        Assert.Equal("mixed", cut.Find(".epos-wahlkaestchen--kopf").GetAttribute("aria-checked"));

        cut.Find(".epos-wahlkaestchen--kopf").Click();                                  // Teil -> alle
        Assert.Equal("true", cut.Find(".epos-wahlkaestchen--kopf").GetAttribute("aria-checked"));
        cut.Find(".epos-wahlkaestchen--kopf").Click();                                  // alle -> keine sichtbare
        Assert.Equal(new[] { "D" }, wahl.Gewaehlte);                                    // verborgene bleibt
    }

    /// <summary>Kästchen schaltet, Umschalt+Klick wählt den Bereich vom Anker.</summary>
    [Fact]
    public void Kaestchen_schaltet_und_Umschalt_waehlt_den_Bereich()
    {
        var wahl = new Bereichswahl();
        var sichtbar = new[] { "1", "2", "3", "4" };
        var cut = Aufbauen(mehr: p => p
            .Add(x => x.ProjektWahl, wahl)
            .Add(x => x.ProjektSichtbar, sichtbar)
            .Add(x => x.Links, Kaestchenliste(Auswahlbereich.Projekt, sichtbar)));

        var zeilen = cut.FindAll(".epos-zweispalten-bereich--projekt .epos-wahlkaestchen:not(.epos-wahlkaestchen--kopf)");
        zeilen[0].Click();
        cut.FindAll(".epos-zweispalten-bereich--projekt .epos-wahlkaestchen:not(.epos-wahlkaestchen--kopf)")[2]
           .Click(new MouseEventArgs { ShiftKey = true });
        Assert.Equal(new[] { "1", "2", "3" }, wahl.Gewaehlte);
        Assert.Equal("true", cut.FindAll(".epos-zweispalten-bereich--projekt .epos-wahlkaestchen:not(.epos-wahlkaestchen--kopf)")[1].GetAttribute("aria-checked"));
        Assert.Equal("1", wahl.Zuletzt);   // der Anker bleibt

        cut.FindAll(".epos-zweispalten-bereich--projekt .epos-wahlkaestchen:not(.epos-wahlkaestchen--kopf)")[1].Click();
        Assert.Equal(new[] { "1", "3" }, wahl.Gewaehlte);
    }

    /// <summary>Strg+A in einer Liste wählt deren gefilterte Liste (aus dem Skript).</summary>
    [Fact]
    public void StrgA_waehlt_die_gefilterte_Liste()
    {
        var wahl = new Bereichswahl();
        var cut = Aufbauen(mehr: p => p.Add(x => x.KatalogWahl, wahl).Add(x => x.KatalogSichtbar, new[] { "x", "y" }));
        Assert.Equal("1", cut.Find(".epos-zweispalten-bereich--katalog").GetAttribute("data-mehrfach"));
        Assert.Null(cut.Find(".epos-zweispalten-bereich--projekt").GetAttribute("data-mehrfach"));
        cut.InvokeAsync(() => cut.Instance.ListenTaste("Katalog", "Alle"));
        Assert.Equal(new[] { "x", "y" }, wahl.Gewaehlte);
    }

    /// <summary>Die Regel der Bereichswahl: Klick allein, Strg schaltet, Umschalt Bereich, Bereinigen.</summary>
    [Fact]
    public void Bereichswahl_Klick_Strg_Umschalt_und_Bereinigen()
    {
        var w = new Bereichswahl();
        var s = new[] { "a", "b", "c", "d" };
        w.Klick("b", false, false, s);
        Assert.Equal(new[] { "b" }, w.Gewaehlte);
        w.Klick("d", false, true, s);
        Assert.Equal(new[] { "b", "c", "d" }, w.Gewaehlte);
        w.Klick("a", true, false, s);
        Assert.Equal(new[] { "b", "c", "d", "a" }, w.Gewaehlte);
        w.Klick("c", true, false, s);
        Assert.Equal(new[] { "b", "d", "a" }, w.Gewaehlte);
        w.Klick("a", false, false, s);
        Assert.Equal(new[] { "a" }, w.Gewaehlte);
        Assert.Equal(Kopfwahl.Teil, w.Kopfstand(s));
        w.Bereinigen(new[] { "b" });
        Assert.Empty(w.Gewaehlte);
        Assert.Null(w.Zuletzt);
    }

    // =====================================================================
    // Die Regel im Stilblatt - eine bunit-Probe sieht sie nicht (Lehre W6-B-1)
    // =====================================================================

    /// <summary>Kennfarben an oberer Rahmenkante und Marke (KA‑E‑10); Knöpfe ohne Kennfarbe.</summary>
    [Fact]
    public void Die_Kennfarben_stehen_an_Rahmen_und_Marke_nicht_an_den_Knoepfen()
    {
        Assert.Contains("var(--epos-schema-versorgung)", Stilblock(".epos-zweispalten-bereich--projekt {"));
        Assert.Contains("var(--epos-quelle-rahmen)", Stilblock(".epos-zweispalten-bereich--katalog {"));
        Assert.Contains("var(--epos-senke-rahmen)", Stilblock(".epos-zweispalten-bereich--satz    {"));
        Assert.Contains("border-top: 3px solid var(--epos-zweispalten-kennfarbe", Stilblock(".epos-zweispalten-bereich {"));
        Assert.Contains("background: var(--epos-zweispalten-kennfarbe)", Stilblock(".epos-zweispalten-marke {"));
        Assert.DoesNotContain("kennfarbe", Stilblock(".epos-zweispalten-knopf {"));
    }

    /// <summary>
    /// Mit fester Höhe teilt ein Raster den Inhalt: Projekt (Untergrenze bis Wunsch),
    /// Trennlinie 8 px, Katalog (Rest, mindestens sein Inhalt), Satz; der Dialog rollt nicht.
    /// </summary>
    [Fact]
    public void Mit_fester_Hoehe_teilt_ein_Raster_und_der_Dialog_rollt_nicht()
    {
        string css = Stilblatt();
        string dialog = Block(css, "#app > .epos-dialog:has(> .epos-zweispalten),\n.epos-ueberlagerung-inhalt > .epos-dialog:has(> .epos-zweispalten) {");
        Assert.Contains("height: 100dvh", dialog);
        Assert.Contains("overflow: hidden", dialog);

        string raster = Block(css, ":is(#app, .epos-ueberlagerung-inhalt) > .epos-dialog > .epos-zweispalten {");
        Assert.Contains("display: grid", raster);
        Assert.Contains("minmax(calc(var(--epos-trenner-min)", raster);
        Assert.Contains("8px", raster);
        Assert.Contains("minmax(min-content, 1fr)", raster);
        Assert.Contains("overflow: hidden", raster);

        string projekt = Block(css, ":is(#app, .epos-ueberlagerung-inhalt) > .epos-dialog > .epos-zweispalten .epos-zweispalten-bereich--projekt .epos-raster-huelle {");
        Assert.Contains("flex: 0 1 calc(var(--epos-trenner-hoehe) * var(--epos-zeilenskala, 1))", projekt);
        Assert.Contains("min-height: calc(var(--epos-trenner-min) * var(--epos-zeilenskala, 1))", projekt);
        string katalog = Block(css, ":is(#app, .epos-ueberlagerung-inhalt) > .epos-dialog > .epos-zweispalten .epos-zweispalten-bereich--katalog .epos-raster-huelle {");
        Assert.Contains("min-height: calc(var(--epos-katalog-min) * var(--epos-zeilenskala, 1))", katalog);
        Assert.Contains("max-height: none", katalog);

        Assert.Contains("overflow: auto", Stilblock(".epos-zweispalten-satz {"));
    }

    /// <summary>Die Kopfleisten brechen nicht um (4.8) und sind kein Rollbereich.</summary>
    [Fact]
    public void Die_Kopfleiste_bricht_nicht_um_und_rollt_nicht()
    {
        string leiste = Stilblock(".epos-zweispalten-kopfleiste {");
        Assert.Contains("flex-wrap: nowrap", leiste);
        Assert.Contains("overflow: clip", leiste);
        Assert.DoesNotContain("overflow: auto", leiste);
    }

    /// <summary>Die Haftregel „Dialog im eigenen Fenster" nimmt diese Dialoge aus — sie füllen ihr Fenster.</summary>
    [Fact]
    public void Die_Haftregel_nimmt_die_Katalogauswahl_aus()
    {
        string css = Stilblatt();
        Assert.Contains("#app:has(> .epos-fenstermarke) > .epos-dialog:not(.epos-katalog-dialog, :has(> .epos-zweispalten)) > .epos-dialog-fuss {", css);
        // Das Banner behaelt seine Regel samt Kreuz: im nicht rollenden Dialog steht es ohnehin unter dem Kopf.
        Assert.Contains("#app:has(> .epos-fenstermarke) > .epos-dialog:not(.epos-katalog-dialog) > .epos-warnbanner {", css);
        Assert.Contains("#app:has(> .epos-fenstermarke) > .epos-dialog:not(.epos-katalog-dialog, :has(> .epos-zweispalten)) > .epos-dialog-kopf:first-child", css);
        // In html:has(...) darf die Ausnahme nicht stehen: :has in :has verwirft Chromium samt Regel
        // (dort setzt sie nur das scroll-padding, das ein nicht rollendes Dokument nicht braucht).
        Assert.DoesNotContain("html:has(> body > #app > .epos-dialog:not(.epos-katalog-dialog, :has(", css);
    }

    [Fact]
    public void Die_Bereiche_stehen_untereinander()
    {
        Assert.Contains("flex-direction: column", Stilblock(".epos-zweispalten {"));
        Assert.DoesNotContain("flex-direction: row", Stilblock(".epos-zweispalten {"));
    }

    [Fact]
    public void Der_Block_traegt_keine_Medienabfrage()
    {
        string css = Stilblatt();
        int a = css.IndexOf("ZWEISPALTENAUSWAHL", StringComparison.Ordinal);
        Assert.True(a >= 0, "Der Block Zweispaltenauswahl steht nicht im Stilblatt");
        int e = css.IndexOf("/* ====", a + 20, StringComparison.Ordinal);
        string block = e > a ? css.Substring(a, e - a) : css.Substring(a);
        Assert.DoesNotContain("@media", block);
        Assert.DoesNotContain("flex-wrap", Stilblock(".epos-zweispalten {"));
    }

    [Fact]
    public void Die_Mittelspalte_und_ihre_Klassen_sind_gefallen()
    {
        string css = Stilblatt();
        Assert.DoesNotContain(".epos-zweispalten-mitte {", css);
        Assert.DoesNotContain(".epos-zweispalten-uebernahme {", css);
        Assert.DoesNotContain(".epos-zweispalten-pfeil--breit", css);
        Assert.Contains("--epos-zweispalten-umbruch:", Stilblock(":root {"));
    }

    /// <summary>Ohne feste Höhe (Assistent) bleibt die Projektliste höhenbegrenzt.</summary>
    [Fact]
    public void Ohne_feste_Hoehe_bleibt_die_Projektliste_hoehenbegrenzt()
    {
        Assert.Contains("--epos-projektlistenhoehe: 12rem;", Stilblock(":root {"));
        Assert.Contains("max-height: var(--epos-projektlistenhoehe)",
                        Stilblock(".epos-zweispalten-spalte--oben .epos-raster-huelle {"));
    }

    // =====================================================================
    // Die Knopfleiste unter der Katalogliste (Befund W12-B-1)
    // =====================================================================

    /// <summary>
    /// <b>Die Regel zu Befund W12‑B‑1.</b> Die Knopfleiste bricht um, und ein
    /// Knopf bemisst sich an seiner Beschriftung — beides steht EINMAL im Blatt
    /// (<c>.epos-leiste</c> / <c>.epos-knopf</c>) und gilt damit für alle elf
    /// Projekt/DB-Dialoge wie für die Katalogverwaltungen.
    ///
    /// <para>Vier Dinge dürfen NICHT wiederkommen: eine Reihe ohne Umbruch
    /// (dann schrumpfen die Knöpfe unter ihren Text), <c>white-space: nowrap</c>
    /// am Knopf (dann kann der Text im Knopf nicht umbrechen),
    /// <c>overflow: hidden</c> (das schneidet die Beschriftung ab, statt sie zu
    /// zeigen) und eine feste <c>width</c> (dann bemisst sich der Knopf nicht
    /// mehr an seiner Beschriftung — <c>min-width</c> bleibt erlaubt, es ist
    /// ein Mindestmaß).</para>
    /// </summary>
    [Fact]
    public void Die_Knopfleiste_bricht_um_statt_die_Beschriftung_abzuschneiden()
    {
        string leiste = Stilblock(".epos-leiste {");
        Assert.Contains("flex-wrap: wrap", leiste);
        Assert.Contains("gap:", leiste);            // Abstand ueber gap, nicht margin

        string knopf = Stilblock(".epos-knopf {");
        Assert.Contains("flex: 0 1 auto", knopf);   // bemisst sich am Text
        Assert.Contains("white-space: normal", knopf);
        Assert.Contains("overflow-wrap: break-word", knopf);

        Assert.DoesNotContain("white-space: nowrap", knopf);
        Assert.DoesNotContain("overflow: hidden", knopf);

        // min-width und max-width bleiben erlaubt; eine blanke width nicht.
        Assert.False(Regex.IsMatch(knopf, @"(?<![\w-])width\s*:"),
                     "Der Hausknopf traegt eine feste Breite - er soll sich an "
                     + "seiner Beschriftung bemessen (Befund W12-B-1).");
    }

    /// <summary>
    /// <b>Befund W12‑B‑1</b> vom 05.09.2026 (Windows-Abnahme, Dialog „Standard
    /// Stromprofil"): „Beschriftung der Buttons nicht zur Umrandung passen."
    /// Unter der rechten Liste stehen dort VIER Knöpfe — „Stromverbraucher
    /// ändern…", „… neu…", „… löschen", „Typ in DB ändern…" — in einer Spalte,
    /// die nur die halbe Dialogbreite hat. Der Text lief über den Rahmen in den
    /// Nachbarknopf, die Umrandung lag mitten im Wort.
    ///
    /// <para>Diese Probe hält das MARKUP fest: Die Knöpfe, die ein Aufrufer
    /// unter seine Katalogliste setzt, landen in der rechten Spalte des
    /// Bausteins und stehen dort in <c>.epos-leiste</c> — genau der Klasse, an
    /// der die Umbruchregel hängt. Die Regel selbst prüft
    /// <see cref="Die_Knopfleiste_bricht_um_statt_die_Beschriftung_abzuschneiden"/>;
    /// eine bunit-Probe sieht ein Stilblatt nicht (Lehre W6‑B‑1).</para>
    /// </summary>
    [Fact]
    public void Die_Knopfleiste_unter_der_Katalogliste_traegt_die_Leistenklasse()
    {
        string[] beschriftungen =
        {
            "Stromverbraucher ändern...",
            "Stromverbraucher neu...",
            "Stromverbraucher löschen",
            "Typ in DB ändern...",
        };

        var cut = Render<Zweispaltenauswahl>(p => p
            .Add(x => x.LinksTitel, "Projekt Strombedarf:")
            .Add(x => x.RechtsTitel, "Datenbank Strombedarf:")
            .Add(x => x.Rechts, (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.AddAttribute(1, "class", "epos-leiste");
                int schluessel = 2;
                foreach (string text in beschriftungen)
                {
                    b.OpenElement(schluessel++, "button");
                    b.AddAttribute(schluessel++, "type", "button");
                    b.AddAttribute(schluessel++, "class", "epos-knopf");
                    b.AddContent(schluessel++, text);
                    b.CloseElement();
                }
                b.CloseElement();
            })));

        // Die Leiste steht IM UNTEREN Block - nicht daneben, nicht im Wirt.
        IElement leiste = cut.Find(".epos-zweispalten-spalte--unten > .epos-zweispalten-inhalt > .epos-leiste");

        // ... und alle vier Knöpfe stehen in dieser einen Leiste.
        var knoepfe = leiste.QuerySelectorAll("button.epos-knopf");
        Assert.Equal(beschriftungen.Length, knoepfe.Length);
        Assert.Equal(beschriftungen, knoepfe.Select(k => k.TextContent).ToArray());

        // Kein Knopf trägt eine Breite am Element - die Bemessung gehört ins
        // Stilblatt, und ein Inline-Stil wäre gegen die Hausregel.
        Assert.All(knoepfe, k => Assert.Null(k.GetAttribute("style")));
    }

    /// <summary>
    /// Dieselbe Leiste in allen elf Projekt/DB-Dialogen: Wer Katalogknöpfe unter
    /// seine rechte Liste setzt, nimmt <c>.epos-leiste</c>. Sonst gäbe es eine
    /// zweite Knopfleiste, und die bekäme den Umbruch aus W12‑B‑1 nicht mit.
    /// </summary>
    [Fact]
    public void Jeder_Dialog_setzt_seine_Katalogknoepfe_in_eine_epos_leiste()
    {
        foreach (string d in Dialoge)
        {
            string quelle = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", d))
                                .Replace("\r\n", "\n");

            int a = quelle.IndexOf("<Rechts>", StringComparison.Ordinal);
            int e = quelle.IndexOf("</Rechts>", StringComparison.Ordinal);
            Assert.True(a >= 0 && e > a, d + ": kein <Rechts>-Block");

            string rechts = quelle.Substring(a, e - a);
            if (!rechts.Contains("epos-knopf", StringComparison.Ordinal))
                continue;                       // Dialog ohne Katalogknoepfe

            Assert.Contains("class=\"epos-leiste\"", rechts, StringComparison.Ordinal);
        }
    }

    // =====================================================================
    // Der Bestand - kein Dialog baut das Muster noch selbst
    // =====================================================================

    /// <summary>
    /// Die elf Projekt/Datenbank-Dialoge des Hauses. Wer einen zwölften baut,
    /// nimmt den Baustein — und trägt ihn hier ein.
    /// </summary>
    private static readonly string[] Dialoge =
    {
        "Dialoge/Bedarf/GebaeudeDialog.razor",
        "Dialoge/Bedarf/WaermebedarfExternDialog.razor",
        "Dialoge/Bedarf/BedarfsProfileDialog.razor",
        "Dialoge/Erzeuger/HeizkesselDialog.razor",
        "Dialoge/Erzeuger/BhkwDialog.razor",
        "Dialoge/Erzeuger/PhotovoltaikDialog.razor",
        "Dialoge/Erzeuger/PufferspeicherDialog.razor",
        "Dialoge/Erzeuger/StromspeicherDialog.razor",
        "Dialoge/Solarthermie/SolarkollektorenDialog.razor",
        "Dialoge/Solarthermie/SolarganglinieDialog.razor",
        "Dialoge/Strom/StromganglinieDialog.razor",
    };

    /// <summary>Alle zwölf Wirte: die elf oben und der Wärmepumpendialog (ohne Katalogknöpfe).</summary>
    private static readonly string[] Wirte =
        Dialoge.Append("Dialoge/Waermepumpe/WaermepumpenDialog.razor").ToArray();

    [Fact]
    public void Alle_zwoelf_Projekt_DB_Dialoge_nehmen_den_Baustein_mit_Dialognamen_und_Satz()
    {
        foreach (string d in Wirte)
        {
            string quelle = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", d));
            Assert.Contains("<Zweispaltenauswahl Dialogname=\"", quelle);
            Assert.Contains("<Satz>", quelle);
        }
    }

    /// <summary>
    /// Die alte Pfeilspalte ist weg — sonst gäbe es zwei Fassungen desselben
    /// Musters, und die eine bekäme den nächsten Anwenderwunsch nicht mit.
    /// </summary>
    [Fact]
    public void Keine_Komponente_baut_die_Pfeilspalte_noch_selbst()
    {
        var uebrig = Directory
            .EnumerateFiles(Path.Combine(Wurzel(), "EPOS.UI"), "*.razor", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("epos-auswahlpfeile", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(uebrig);
        Assert.DoesNotContain("epos-auswahlpfeile", Stilblatt());
    }

    // =====================================================================
    // Hilfen
    // =====================================================================

    private static string Wurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return d!.FullName;
    }

    /// <summary>
    /// Das Hausblatt mit angeglichenen Zeilenenden: Auf Windows liegt es nach
    /// dem Auschecken mit CRLF (.gitattributes: text=auto), ein zweizeiliger
    /// Selektor traegt hier aber "\n" - dieselbe Angleichung wie in
    /// StartseiteTests und StilblattTests.
    /// </summary>
    private static string Stilblatt()
        => File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-ui.css"))
               .Replace("\r\n", "\n");

    /// <summary>Liest den Rumpf einer Regel aus dem Stilblatt.</summary>
    private static string Stilblock(string selektor)
        => Block(Stilblatt(), selektor.Replace("\r\n", "\n"));

    private static string Block(string css, string selektor)
    {
        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }

    // =================================================================================
    // Stufe 2: Katalog-Fussleiste, Suche in der Kopfleiste, Wahl aus der Katalogliste
    // =================================================================================

    private static RenderFragment Knopf(string klasse, string text) => b =>
    {
        b.OpenElement(0, "button");
        b.AddAttribute(1, "type", "button");
        b.AddAttribute(2, "class", "epos-knopf " + klasse);
        b.AddContent(3, text);
        b.CloseElement();
    };

    [Fact]
    public void Die_Fussleiste_nennt_den_Namen_und_traegt_Neu_rechts()
    {
        var cut = Render<Zweispaltenauswahl>(p => p
            .Add(x => x.Dialogname, "")
            .Add(x => x.LinksTitel, "Projekt").Add(x => x.RechtsTitel, "Katalog")
            .Add(x => x.KatalogFuss, Knopf("k-loeschen", "Löschen"))
            .Add(x => x.KatalogFussRechts, Knopf("k-neu", "Neu…"))
            .Add(x => x.KatalogFussName, "Kessel A"));

        IElement fuss = cut.Find(".epos-zweispalten-bereich--katalog > .epos-zweispalten-fussleiste");
        var kinder = fuss.Children.ToList();
        Assert.Equal("Kessel A:", kinder[0].TextContent);
        Assert.Contains("epos-zweispalten-fussname", kinder[0].ClassList);
        Assert.Contains("k-loeschen", kinder[1].ClassList);
        Assert.Contains("epos-leiste-fueller", kinder[2].ClassList);
        Assert.Contains("k-neu", kinder[3].ClassList);
    }

    [Fact]
    public void Bei_Mehrfachwahl_nennt_die_Fussleiste_die_Zahl()
    {
        var wahl = new Bereichswahl();
        wahl.Setzen(new[] { "a", "b", "c" });
        var cut = Render<Zweispaltenauswahl>(p => p
            .Add(x => x.Dialogname, "")
            .Add(x => x.KatalogWahl, wahl)
            .Add(x => x.KatalogSichtbar, new[] { "a", "b", "c" })
            .Add(x => x.KatalogFuss, Knopf("k-loeschen", "Löschen"))
            .Add(x => x.KatalogFussName, "Kessel A"));

        Assert.Equal(string.Format(Resource.Culture, Resource.AUSWAHL_FUSS_SAETZE, 3),
                     cut.Find(".epos-zweispalten-fussname").TextContent);
    }

    [Fact]
    public void Ohne_Fussabschnitte_gibt_es_keine_Fussleiste()
    {
        var cut = Render<Zweispaltenauswahl>(p => p.Add(x => x.Dialogname, "").Add(x => x.KatalogFussName, "Kessel A"));
        Assert.Empty(cut.FindAll(".epos-zweispalten-fussleiste"));
    }

    /// <summary>
    /// <b>Ein eigenes Zeilenmaß des Katalogs</b> (Kästchenmodus 46 px) setzt die Untergrenze auf Kopf
    /// und ZWEI Zeilen und markiert den Baustein für die Regel unter 600 px (Konzept 4.8).
    /// </summary>
    [Fact]
    public void Ein_eigenes_Katalogzeilenmass_haelt_zwei_Katalogzeilen()
    {
        var cut = Render<Zweispaltenauswahl>(p => p.Add(x => x.Dialogname, "").Add(x => x.KatalogZeile, 46));
        var wurzel = cut.Find(".epos-zweispalten");
        Assert.Contains("epos-zweispalten--zeilenmass", wurzel.ClassList);
        string stil = wurzel.GetAttribute("style") ?? "";
        Assert.Contains("--epos-katalog-min: 147px", stil);
        Assert.Contains("--epos-katalog-zeile: 46px", stil);

        var ohne = Render<Zweispaltenauswahl>(p => p.Add(x => x.Dialogname, ""));
        Assert.DoesNotContain("epos-zweispalten--zeilenmass", ohne.Find(".epos-zweispalten").ClassList);
        Assert.Contains("--epos-katalog-min: " + Zweispaltenauswahl.KATALOG_MINDESTHOEHE + "px",
                        ohne.Find(".epos-zweispalten").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Bereichswahl_Setzen_uebernimmt_die_Liste_der_Kaestchen()
    {
        var wahl = new Bereichswahl();
        wahl.Setzen(new[] { "x", "y", "x" });
        Assert.Equal(new[] { "x", "y" }, wahl.Gewaehlte);
        Assert.Equal("y", wahl.Zuletzt);
        wahl.Setzen(Array.Empty<string>());
        Assert.Equal(0, wahl.Anzahl);
    }

    /// <summary>
    /// <b>Die Suchzeile steht in der Kopfleiste des Katalogs</b> (Konzept 4.2, 4.8): Die
    /// Katalogliste im Abschnitt Rechts reicht sie hinauf; Suche und Trefferzahl wirken wie
    /// zuvor, und über der Liste steht keine zweite Zeile mehr.
    /// </summary>
    [Fact]
    public void Die_Suchzeile_der_Katalogliste_steht_in_der_Katalog_Kopfleiste()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var profil = Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel, s => Resource.ResourceManager.GetString(s) ?? s);
        var zeilen = new[]
        {
            new Katalogfilterzeile(1, "Kessel A").MitText(Katalogfilterprofil.SpBezeichner, "Kessel A"),
            new Katalogfilterzeile(2, "Kessel B").MitText(Katalogfilterprofil.SpBezeichner, "Kessel B"),
        };
        var stand = new Katalogfilterstand();
        RenderFragment liste = b =>
        {
            b.OpenComponent<Katalogliste>(0);
            b.AddAttribute(1, nameof(Katalogliste.Profil), profil);
            b.AddAttribute(2, nameof(Katalogliste.Zeilen), (IReadOnlyList<Katalogfilterzeile>)zeilen);
            b.AddAttribute(3, nameof(Katalogliste.Filterstand), stand);
            b.CloseComponent();
        };
        var cut = Render<Zweispaltenauswahl>(p => p.Add(x => x.Dialogname, "").Add(x => x.Rechts, liste));

        IElement kopf = cut.Find(".epos-zweispalten-bereich--katalog > .epos-zweispalten-kopfleiste");
        Assert.NotNull(kopf.QuerySelector(".epos-katalog-suchzeile input[type=search]"));
        Assert.Empty(cut.FindAll(".epos-katalogliste .epos-katalog-suchzeile"));
        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));

        cut.Find(".epos-zweispalten-kopfleiste input[type=search]").Input("Kessel B");
        Assert.StartsWith("1", cut.Find(".epos-zweispalten-kopfleiste .epos-katalog-treffer").TextContent);
    }

    // =====================================================================
    // Kompaktstufe und Rollbalken (KB1, Konzept 4.8)
    // =====================================================================

    /// <summary>
    /// Die Kompaktstufe ist EINE Skalenebene: Unter 1 200 px Breite oder 800 px Höhe setzt die
    /// Medienabfrage die Token des Hauses am Dialog mit dem Baustein (und an der
    /// Kältemaschinenauswahl) neu — Schrift 12 px, Kartentitel 14 px, Berührungsziel 37 px,
    /// Zeilenskala 46/53 —, keine zweite Kopie der Regeln.
    /// </summary>
    [Fact]
    public void Die_Kompaktstufe_setzt_die_Token_unter_1200_mal_800_px()
    {
        string css = Stilblatt();
        const string abfrage = "@media (max-width: 1199.98px), (max-height: 799.98px) {";
        int a = css.IndexOf(abfrage, StringComparison.Ordinal);
        Assert.True(a >= 0, "Die Medienabfrage der Kompaktstufe fehlt");
        string token = Block(css.Substring(a), ".epos-dialog:has(> .epos-zweispalten),\n    .epos-dialog.epos-kaeltemaschine-admin {");
        Assert.Contains("--epos-schriftgroesse: 12px", token);
        Assert.Contains("--epos-schriftgroesse-kartentitel: 14px", token);
        Assert.Contains("--epos-touchziel: 37px", token);
        Assert.Contains("--epos-zeilenskala: 0.8679", token);
        Assert.Contains("font-size: var(--epos-schriftgroesse)", token);
        // 53 px × Skala = 46 px (Projektzeile), 46 px × Skala = 40 px (Katalogzeile mit Zeilenmaß).
        Assert.Equal(46, (int)Math.Round(53 * 0.8679));
        Assert.Equal(40, (int)Math.Round(46 * 0.8679));
    }

    /// <summary>
    /// Die Zeilenmaße durchlaufen die Skala: das gesetzte Maß der virtualisierten Liste, der Kasten
    /// der Zeilenwahl und des Wahlkästchens, die Grenzen der Trennlinie und die Untergrenze des
    /// Katalogs, die der Wirt über <c>KatalogZeile</c> gibt — die Zahl im Programm bleibt in der
    /// Normalstufe, das Stilblatt rechnet um.
    /// </summary>
    [Fact]
    public void Die_Zeilenmasse_durchlaufen_die_Zeilenskala()
    {
        string css = Stilblatt();
        Assert.Contains("height: calc(var(--epos-rasterzeile, 53px) * var(--epos-zeilenskala, 1));", css);
        Assert.Contains("height: calc(46px * var(--epos-zeilenskala, 1) - 1px);", Stilblock(".epos-zeilenzelle {"));
        Assert.Contains("height: calc(46px * var(--epos-zeilenskala, 1) - 1px);", Stilblock(".epos-kaestchenzelle {"));
        string raster = Block(css, ":is(#app, .epos-ueberlagerung-inhalt) > .epos-dialog > .epos-zweispalten {");
        Assert.Contains("calc(var(--epos-trenner-min) * var(--epos-zeilenskala, 1) + var(--epos-touchziel) + 18px)", raster);

        // Der Wirt gibt sein Zeilenmaß als Zahl der Normalstufe; die Untergrenze skaliert im Stilblatt.
        IRenderedComponent<Zweispaltenauswahl> cut = Render<Zweispaltenauswahl>(p => p
            .Add(x => x.KatalogZeile, 46)
            .Add(x => x.Rechts, (RenderFragment)(b => b.AddMarkupContent(0, "<p>K</p>"))));
        Assert.Contains("--epos-katalog-min: 147px", Stil(cut));
        Assert.Contains("min-height: calc(var(--epos-katalog-min) * var(--epos-zeilenskala, 1))",
            Block(css, ":is(#app, .epos-ueberlagerung-inhalt) > .epos-dialog > .epos-zweispalten .epos-zweispalten-bereich--katalog .epos-raster-huelle {"));
    }

    /// <summary>
    /// Der Rollbalken gilt nur unter der Mindesthöhe: Der Baustein steht im eigenen Fenster auf der
    /// gemessenen Summe seiner Untergrenzen (--epos-zweispalten-min), und allein der Dialog mit
    /// <c>data-zweispalten-eng</c> rollt senkrecht. Die Klemme auf eine Katalogzeile gilt nur in
    /// der Normalstufe, die aufgeklappte Detailzeile hat eine Obergrenze.
    /// </summary>
    [Fact]
    public void Der_Dialogkoerper_rollt_nur_unter_der_Mindesthoehe()
    {
        string css = Stilblatt();
        Assert.Contains("min-height: var(--epos-zweispalten-min, 0px)", Block(css, "#app > .epos-dialog > .epos-zweispalten {"));
        string eng = Block(css, "#app > .epos-dialog[data-zweispalten-eng]:has(> .epos-zweispalten) {");
        Assert.Contains("overflow-y: auto", eng);
        Assert.Contains("overflow-x: hidden", eng);
        Assert.Contains("@media (min-width: 1200px) and (min-height: 800px) {\n    @container katalogauswahl (max-height: 599px)", css);
        Assert.Contains("max-height: var(--epos-satz-max, 45vh)",
            Block(css, ":is(#app, .epos-ueberlagerung-inhalt) > .epos-dialog > .epos-zweispalten > .epos-zweispalten-bereich--satz .epos-zweispalten-satz {"));

        // Das Skript misst und schaltet; es traegt keine Pixelzahl der Zeilen.
        string js = File.ReadAllText(Path.Combine(Wurzel(), "EPOS.UI", "wwwroot", "epos-zweispalten.js"));
        Assert.Contains("export function mindesthoehe(wurzel)", js);
        Assert.Contains("\"--epos-zweispalten-min\"", js);
        Assert.Contains("\"data-zweispalten-eng\"", js);
        Assert.Contains("--epos-zeilenskala", js);
        Assert.DoesNotContain("53", js);
        Assert.DoesNotContain("46", js);
    }
}
