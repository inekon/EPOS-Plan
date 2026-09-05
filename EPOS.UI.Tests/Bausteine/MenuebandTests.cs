using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// NACHWEIS N4 (Vermessung § 11.8) — das Menueband des Hauptfensters.
///
/// <para>Geprueft wird genau das, was der Rueckbau von <c>Hauptfensterrahmen</c>
/// aufgibt: die VOLLZAEHLIGKEIT der Punkte, ihre BESCHRIFTUNG in beiden
/// Sprachen und die Zusicherung, dass jeder Klick einen
/// <see cref="Seitenschluessel"/> meldet — und nichts sonst. Fiele einer der
/// 55 Punkte beim Umzug aus, saehe man es an keiner anderen Stelle mehr; der
/// Designer, der ihn bisher belegte, ist mit W16c.3 geloescht.</para>
///
/// <para>ANWENDERENTSCHEID W16c-E-2 (04.09.2026): Die zwei Sprachpunkte
/// haengen seither unter dem Kopf „Sprache" statt in der obersten Ebene. Das
/// aendert drei Zahlen dieses Nachweises — 55 statt 54 Punkte, VIER statt
/// fuenf Koepfe, 13 statt 12 aufklappende — und den Sprachfall: Der Kopf will
/// erst geoeffnet werden.</para>
///
/// <para>Die Sprache wird JE FALL gepinnt (Regel seit iU9-W8): Die
/// Beschriftungen kommen aus <c>MyResource</c>, und der Windows-Laeufer laeuft
/// englisch. Der Zweisprachenfall setzt die Kultur selbst und stellt sie
/// zurueck.</para>
/// </summary>
public class MenuebandTests : BunitContext
{
    private readonly CultureInfo _vorher = CultureInfo.CurrentUICulture;

    public MenuebandTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Kultur("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        Kultur(_vorher.Name.Length == 0 ? "de-DE" : _vorher.Name);
        base.Dispose(disposing);
    }

    private static void Kultur(string name)
    {
        var kultur = new CultureInfo(name);
        CultureInfo.DefaultThreadCurrentCulture = kultur;
        CultureInfo.DefaultThreadCurrentUICulture = kultur;
        Thread.CurrentThread.CurrentCulture = kultur;
        Thread.CurrentThread.CurrentUICulture = kultur;
        CultureInfo.CurrentCulture = kultur;
        CultureInfo.CurrentUICulture = kultur;
    }

    private static IReadOnlyList<Menuepunkt> Punkte =>
        Menuetabelle.Alle.Where(p => !p.Trenner).ToList();

    // =====================================================================
    //  Vollzaehligkeit
    // =====================================================================

    [Fact]
    public void Die_Tabelle_fuehrt_alle_Menuepunkte_des_Bestands()
    {
        // 45 aus MDIMainForm.Designer.cs (dort als ToolStripMenuItem gezaehlt)
        // und 9 aus den acht Init*-Methoden von MDIMainForm.cs. Die Zahl 45 der
        // Arbeitsanweisung ist die DESIGNER-Zahl; das Menue des laufenden
        // Programms hatte immer 54 Punkte (Befund W16c-B2). Der 55. ist der
        // Kopf "Sprache" des Anwenderentscheids W16c-E-2 (04.09.2026) - er
        // kommt hinzu, es faellt keiner weg.
        Assert.Equal(55, Punkte.Count);

        // Sechs Trenner standen im Designer, zwei haengten BaueVariantenMenue
        // und InitKiHilfe programmatisch ein. W16c-E-2 bringt keinen neuen.
        Assert.Equal(8, Menuetabelle.Alle.Count(p => p.Trenner));

        // VIER Koepfe der obersten Ebene: Projekt, Administration, Hilfe,
        // Sprache. Im Bestand waren es fuenf (menuToolbar.Items.AddRange =
        // Projekt, Administration, Hilfe, Deutsch, Englisch); die zwei
        // Sprachpunkte sind mit W16c-E-2 unter EINEN Kopf gewandert.
        Assert.Equal(4, Menuetabelle.Eintraege.Count);
        Assert.Equal(new[] { "Projekte", "Administration", "Help", "Sprache" },
                     Menuetabelle.Eintraege.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Der_Kopf_Sprache_traegt_die_zwei_Sprachpunkte_des_Bestands()
    {
        // Anwenderentscheid W16c-E-2: Der Kopf steht dort, wo bis dahin
        // "Deutsch" stand - ganz rechts, nach "Hilfe". Er klappt nur auf; die
        // zwei Punkte behalten Namen, Bild und Seitenschluessel, damit
        // help_mapping.txt und HauptfensterHuelle.Weg weiter greifen.
        Menuepunkt sprache = Menuetabelle.Eintraege[^1];

        Assert.Equal("Sprache", sprache.Name);
        Assert.Equal("MENU_SPRACHE", sprache.TextSchluessel);
        Assert.True(string.IsNullOrEmpty(sprache.Ziel));
        Assert.True(sprache.Klappt);

        Assert.Equal(2, sprache.Untereintraege.Count);

        Menuepunkt deutsch = sprache.Untereintraege[0];
        Assert.Equal("Deutsch", deutsch.Name);
        Assert.Equal(Seitenschluessel.SpracheDeutsch, deutsch.Ziel);
        Assert.Equal("germany", deutsch.Bild);

        Menuepunkt englisch = sprache.Untereintraege[1];
        Assert.Equal("Englisch", englisch.Name);
        Assert.Equal(Seitenschluessel.SpracheEnglisch, englisch.Ziel);
        Assert.Equal("usa", englisch.Bild);
    }

    [Fact]
    public void Jeder_Punkt_traegt_einen_eindeutigen_Namen()
    {
        // Der Name ist der Bezeichner des Vorlaeufers und der Anker fuer
        // help_mapping.txt; zwei gleiche waeren zwei Ziele an einer Kennung.
        List<string> namen = Menuetabelle.Alle.Select(p => p.Name).ToList();
        Assert.Equal(namen.Count, namen.Distinct(StringComparer.Ordinal).Count());
    }

    // =====================================================================
    //  Jeder Klick ist ein Seitenschluessel
    // =====================================================================

    [Fact]
    public void Jeder_klickbare_Punkt_meldet_einen_bekannten_Seitenschluessel()
    {
        var bekannt = new HashSet<string>(Seitenschluessel.Alle, StringComparer.Ordinal);

        foreach (Menuepunkt p in Punkte)
        {
            if (p.Klappt)
            {
                // Ein Untermenue klappt nur auf - es darf gar kein Ziel haben,
                // sonst taete ein Klick zweierlei.
                Assert.True(string.IsNullOrEmpty(p.Ziel),
                            p.Name + " klappt auf UND traegt ein Ziel.");
                continue;
            }

            Assert.False(string.IsNullOrEmpty(p.Ziel), p.Name + " hat kein Ziel.");
            Assert.True(bekannt.Contains(p.Ziel),
                        p.Name + " zeigt auf den unbekannten Schluessel " + p.Ziel + ".");
        }
    }

    [Fact]
    public void Die_Blaetter_des_Baums_sind_die_Handlungen()
    {
        // 42 der 55 Punkte handeln, 13 klappen nur auf (Projekt,
        // Administration, die sieben Untermenues der Administration, PV,
        // Solarkollektoren, Hilfe und - seit W16c-E-2 - Sprache). Der
        // Vorlaeufer fuehrte dafuer 34 Designer-Handler und neun Lambdas in den
        // Init*-Methoden - die Differenz von einem ist MenuItem_PV_Import_PAN,
        // dessen Handler zu KEINEM Steuerelement gehoerte (Befund W16-B24).
        // Der neue Kopf handelt NICHT: Die Zahl der Handlungen bleibt 42.
        Assert.Equal(13, Punkte.Count(p => p.Klappt));
        Assert.Equal(42, Punkte.Count(p => !p.Klappt));
    }

    [Fact]
    public void Nur_der_PV_Import_traegt_ein_Argument()
    {
        // Masken.PvImport ist der einzige Schluessel des Bestands mit einer
        // Zusatzangabe ("CEC" bzw. "PAN"). Der zweite Menuepunkt dafuer
        // (MenuItem_PV_Import_PAN) stand in KEINEM Designer - sein Handler war
        // tot (Befund W16-B24) und faellt mit W16c.3.
        Menuepunkt[] mitArgument = Punkte.Where(p => p.Argument.Length > 0).ToArray();

        Assert.Single(mitArgument);
        Assert.Equal("MenuItem_PV_Import_CEC", mitArgument[0].Name);
        Assert.Equal(Seitenschluessel.PvImport, mitArgument[0].Ziel);
        Assert.Equal("CEC", mitArgument[0].Argument);
    }

    [Fact]
    public void Der_KI_Assistent_haengt_am_Hilfemenue_und_traegt_F1()
    {
        // Befund W16-B23: InitKiHilfe suchte das Hilfemenue ueber den
        // ANZEIGETEXT ("Hilfe" bzw. "Help") - die einzige Stelle des Bestands,
        // an der ein Anzeigetext als Schluessel diente. Hier ist es eine Zeile
        // der Tabelle.
        Menuepunkt hilfe = Menuetabelle.Eintraege.Single(p => p.Name == "Help");
        Menuepunkt ki = hilfe.Untereintraege.Single(p => p.Name == "MenuItem_KiAssistent");

        Assert.Equal(Seitenschluessel.KiAssistent, ki.Ziel);
        Assert.Equal("F1", ki.Kuerzel);
    }

    // =====================================================================
    //  Die Beschriftungen — deutsch UND englisch
    // =====================================================================

    [Fact]
    public void Jede_Beschriftung_steht_in_MyResource_und_zwar_zweisprachig()
    {
        var deutsch = new Dictionary<string, string>(StringComparer.Ordinal);
        var englisch = new Dictionary<string, string>(StringComparer.Ordinal);

        Kultur("de-DE");
        foreach (Menuepunkt p in Punkte) deutsch[p.Name] = p.Text;

        Kultur("en-US");
        foreach (Menuepunkt p in Punkte) englisch[p.Name] = p.Text;

        Kultur("de-DE");

        foreach (Menuepunkt p in Punkte)
        {
            // Der Rueckfall von Menuepunkt.TextFuer ist der SCHLUESSEL selbst -
            // steht er da, gibt es den Eintrag im Katalog nicht.
            Assert.NotEqual(p.TextSchluessel, deutsch[p.Name]);
            Assert.NotEqual(p.TextSchluessel, englisch[p.Name]);

            Assert.False(string.IsNullOrWhiteSpace(deutsch[p.Name]), p.Name + " ohne deutschen Text.");
            Assert.False(string.IsNullOrWhiteSpace(englisch[p.Name]), p.Name + " ohne englischen Text.");
        }

        // Stichproben aus beiden Sprachen - woertlich aus MDIMainForm.resx
        // bzw. .en-US.resx uebernommen.
        Assert.Equal("Neu...", deutsch["MenuItem_ProjektNeu"]);
        Assert.Equal("New...", englisch["MenuItem_ProjektNeu"]);
        Assert.Equal("Klimadaten", deutsch["MenuItem_Klimadaten"]);
        Assert.Equal("Climate data", englisch["MenuItem_Klimadaten"]);
        Assert.Equal("Hilfe", deutsch["Help"]);
        Assert.Equal("Help", englisch["Help"]);

        // Der Kopf aus W16c-E-2 - der einzige Textschluessel des Menues, der
        // KEINE Entsprechung im geloeschten Designer hat.
        Assert.Equal("Sprache", deutsch["Sprache"]);
        Assert.Equal("Language", englisch["Sprache"]);
        Assert.Equal("Deutsch", deutsch["Deutsch"]);
        Assert.Equal("German", englisch["Deutsch"]);
        Assert.Equal("Englisch", deutsch["Englisch"]);
        Assert.Equal("English", englisch["Englisch"]);
    }

    [Fact]
    public void Das_doppelte_Kaufmannsund_des_MenuStrip_ist_ein_einfaches_geworden()
    {
        // WinForms verdoppelt & fuer das Tastenkuerzel ("Daten && Import"); in
        // Razor gibt es diese Verdopplung nicht - dieselbe Angleichung wie bei
        // "Berichte && Kosten" der Startseite (iU9-W16b.2).
        Menuepunkt admin = Menuetabelle.Eintraege.Single(p => p.Name == "Administration");
        Menuepunkt daten = admin.Untereintraege.Single(p => p.Name == "MenuItem_DatImport");

        Assert.Equal("Daten & Import", daten.Text);
        Assert.DoesNotContain("&&", daten.Text, StringComparison.Ordinal);
    }

    // =====================================================================
    //  Darstellung und Bedienung
    // =====================================================================

    [Fact]
    public void Das_Band_zeigt_die_vier_Punkte_der_obersten_Ebene()
    {
        var cut = Render<Menueband>(p => p.Add(x => x.Eintraege, Menuetabelle.Eintraege));

        var knoepfe = cut.FindAll(".epos-menueband > .epos-menueband-punkt > .epos-menueband-knopf, " +
                                  ".epos-menueband > .epos-menueband-knopf");
        Assert.Equal(4, knoepfe.Count);
        Assert.Equal("Projekt", knoepfe[0].TextContent.Trim());
        Assert.Equal("Administration", knoepfe[1].TextContent.Trim());
        Assert.Equal("Hilfe", knoepfe[2].TextContent.Trim());

        // W16c-E-2: "Sprache" steht ganz rechts, wo bis dahin "Deutsch" stand,
        // und klappt auf, statt unmittelbar zu handeln.
        Assert.Equal("Sprache", knoepfe[3].TextContent.Trim());
        Assert.Equal("true", knoepfe[3].GetAttribute("aria-haspopup"));

        // Zugeklappt nennt das Band die zwei Sprachen NICHT mehr.
        string sichtbar = cut.Find(".epos-menueband").TextContent;
        Assert.DoesNotContain("Deutsch", sichtbar, StringComparison.Ordinal);
        Assert.DoesNotContain("Englisch", sichtbar, StringComparison.Ordinal);
    }

    [Fact]
    public void Der_Kopf_Sprache_steht_rechtsbuendig()
    {
        // ANWENDERWUNSCH 05.09.2026 (W16c-E-4): "Sprache soll oben rechts sein"
        // - so wie im Bestand die zwei Sprachpunkte, die als letzte Eintraege
        // des MenuStrip rechtsbuendig am Rand sassen.
        var cut = Render<Menueband>(p => p.Add(x => x.Eintraege, Menuetabelle.Eintraege));

        // Genau EIN Kopf traegt das Kennzeichen, und es ist der letzte.
        var rechts = cut.FindAll(".epos-menueband > .epos-menueband-punkt--rechts");
        Assert.Single(rechts);
        Assert.Equal("Sprache", rechts[0].QuerySelector(".epos-menueband-knopf")!.TextContent.Trim());

        // Die DREI anderen Koepfe tragen es nicht - sie stehen links.
        var koepfe = cut.FindAll(".epos-menueband > .epos-menueband-punkt");
        Assert.Equal(4, koepfe.Count);
        for (int i = 0; i < 3; i++)
            Assert.DoesNotContain("--rechts", koepfe[i].ClassName!, StringComparison.Ordinal);

        // Die REIHENFOLGE im Markup bleibt die des Bestands: Nur die Optik
        // wandert (margin-left: auto), damit Tastaturweg und N4 unberuehrt sind.
        Assert.Equal("menue-Sprache", koepfe[3].QuerySelector(".epos-menueband-knopf")!.Id);
        Assert.True(Menuetabelle.Eintraege[^1].RechtsBuendig);
        Assert.All(Menuetabelle.Eintraege.Take(3), p => Assert.False(p.RechtsBuendig));
    }

    [Fact]
    public void Die_Rechtsbuendigkeit_steht_im_Stilblatt()
    {
        // Eine bunit-Probe sieht nur die Klasse; dass sie auch wirkt, steht im
        // Stilblatt (Muster W5-B-1).
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));

        int a = css.IndexOf(".epos-menueband-punkt--rechts,", StringComparison.Ordinal);
        Assert.True(a >= 0, "Die Regel .epos-menueband-punkt--rechts fehlt im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.Contains("margin-left: auto", css.Substring(a, e - a));
    }

    [Fact]
    public void Ein_Klick_auf_einen_Kopf_klappt_sein_Untermenue_auf()
    {
        var cut = Render<Menueband>(p => p.Add(x => x.Eintraege, Menuetabelle.Eintraege));

        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));

        cut.Find("#menue-Projekte").Click();

        Assert.Single(cut.FindAll(".epos-menueband-klappe"));
        Assert.Equal("true", cut.Find("#menue-Projekte").GetAttribute("aria-expanded"));

        // Sechs Punkte des Designers, zwei aus BaueVariantenMenue, sechs Trenner.
        Assert.Equal(8, cut.FindAll(".epos-menueband-klappe .epos-menueband-zeile").Count);
        Assert.Equal(6, cut.FindAll(".epos-menueband-klappe .epos-menueband-strich").Count);
    }

    [Fact]
    public void Ein_Klick_auf_einen_Punkt_meldet_seinen_Schluessel_und_schliesst()
    {
        Menuepunkt? gemeldet = null;
        var cut = Render<Menueband>(p => p
            .Add(x => x.Eintraege, Menuetabelle.Eintraege)
            .Add(x => x.Gewaehlt, (Menuepunkt m) => gemeldet = m));

        cut.Find("#menue-Projekte").Click();
        cut.Find("#menue-MenuItem_ProjektNeu").Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(Seitenschluessel.ProjektNeu, gemeldet!.Ziel);

        // Nach der Wahl steht das Band wieder zu - der Wirt zeigt womoeglich
        // einen modalen Dialog darueber.
        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
    }

    [Fact]
    public void Ein_Untermenue_der_dritten_Ebene_klappt_seitlich_auf()
    {
        var cut = Render<Menueband>(p => p.Add(x => x.Eintraege, Menuetabelle.Eintraege));

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_Energiesysteme").Click();

        Assert.Single(cut.FindAll(".epos-menueband-klappe--tief"));
        Assert.NotNull(cut.Find("#menue-MenuItem_PV"));
    }

    [Fact]
    public void Ein_Sprachpunkt_im_Untermenue_Sprache_meldet_beim_Klick()
    {
        // ANWENDERENTSCHEID W16c-E-2 (04.09.2026): Bis dahin meldete ein
        // Sprachpunkt der OBERSTEN Ebene unmittelbar. Jetzt geht der Weg ueber
        // den Kopf: aufklappen, waehlen - und das Band steht wieder zu, weil
        // HauptfensterHuelle.SpracheSetzen das Programm neu startet.
        Menuepunkt? gemeldet = null;
        var cut = Render<Menueband>(p => p
            .Add(x => x.Eintraege, Menuetabelle.Eintraege)
            .Add(x => x.Gewaehlt, (Menuepunkt m) => gemeldet = m));

        // Zugeklappt gibt es die Sprachpunkte nicht.
        Assert.Empty(cut.FindAll("#menue-Deutsch"));

        cut.Find("#menue-Sprache").Click();
        Assert.Equal("true", cut.Find("#menue-Sprache").GetAttribute("aria-expanded"));
        Assert.Equal(2, cut.FindAll(".epos-menueband-klappe .epos-menueband-zeile").Count);

        cut.Find("#menue-Deutsch").Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(Seitenschluessel.SpracheDeutsch, gemeldet!.Ziel);
        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
    }

    [Fact]
    public void Der_zweite_Sprachpunkt_meldet_ebenso()
    {
        Menuepunkt? gemeldet = null;
        var cut = Render<Menueband>(p => p
            .Add(x => x.Eintraege, Menuetabelle.Eintraege)
            .Add(x => x.Gewaehlt, (Menuepunkt m) => gemeldet = m));

        cut.Find("#menue-Sprache").Click();
        cut.Find("#menue-Englisch").Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(Seitenschluessel.SpracheEnglisch, gemeldet!.Ziel);
        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
    }

    [Fact]
    public void Die_Pfeiltasten_wandern_ueber_die_vier_Koepfe_und_oeffnen_Sprache()
    {
        // Die Tastatur bedient das Band unveraendert (A-1); nur sind es seit
        // W16c-E-2 VIER Koepfe statt fuenf, und der letzte klappt auf, statt zu
        // handeln. Nach links vom ersten ist der letzte - das ist "Sprache".
        var cut = Render<Menueband>(p => p.Add(x => x.Eintraege, Menuetabelle.Eintraege));
        var band = cut.Find(".epos-menueband");

        band.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("0", cut.Find("#menue-Sprache").GetAttribute("tabindex"));

        band.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("true", cut.Find("#menue-Sprache").GetAttribute("aria-expanded"));
        Assert.NotNull(cut.Find("#menue-Deutsch"));

        // Und wieder nach rechts: der erste Kopf, das Untermenue wandert mit.
        band.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("0", cut.Find("#menue-Projekte").GetAttribute("tabindex"));
        Assert.Equal("true", cut.Find("#menue-Projekte").GetAttribute("aria-expanded"));

        // Ende springt an den letzten Kopf - "Sprache", nicht "Englisch".
        band.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "End" });
        Assert.Equal("0", cut.Find("#menue-Sprache").GetAttribute("tabindex"));
    }

    [Fact]
    public void Escape_klappt_das_offene_Menue_zu()
    {
        var cut = Render<Menueband>(p => p.Add(x => x.Eintraege, Menuetabelle.Eintraege));

        cut.Find("#menue-Administration").Click();
        Assert.Single(cut.FindAll(".epos-menueband-klappe"));

        cut.Find(".epos-menueband").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
    }

    [Fact]
    public void Die_elf_Bilder_des_Bestands_stehen_am_richtigen_Punkt()
    {
        // MDIMainForm.Designer.cs setzte NEUN Image-Zuweisungen (sechs
        // Untermenues, Einstellungen und die zwei Sprachfahnen), zwei weitere
        // kamen aus den Init*-Methoden (Gesetze, Lizenz). Die elf PNG liegen
        // jetzt unter wwwroot/bilder/menue/ und sind dieselben Dateien.
        // W16c-E-2 aendert daran nichts: Die zwei Fahnen sind mit ihren Punkten
        // eine Ebene tiefer gewandert, der neue Kopf "Sprache" traegt KEIN Bild
        // (kein vorhandenes PNG meint das Menue als Ganzes).
        var mitBild = Menuetabelle.Alle.Where(p => p.Bild.Length > 0)
                                       .ToDictionary(p => p.Name, p => p.Bild, StringComparer.Ordinal);

        Assert.Equal(11, mitBild.Count);
        Assert.Equal("Menu1", mitBild["MenuItem_WBundHeizung"]);
        Assert.Equal("germany", mitBild["Deutsch"]);
        Assert.Equal("usa", mitBild["Englisch"]);
        Assert.Equal("lizenzen_32", mitBild["MenuItem_LizenzVerwaltung"]);
    }

    // =====================================================================
    //  BEFUND W16c-B13 (Windows-Abnahme 05.09.2026) — die verschachtelten
    //  Untermenues liessen sich nicht aufklappen
    // =====================================================================

    /// <summary>
    /// Rendert das Band so, wie das Hauptfenster es einhaengt: EIN Woerterbuch
    /// ueber <c>AddMultipleAttributes</c> statt getippter Parameter — derselbe
    /// Weg, den <c>HauptfensterTests.AusHuelle</c> geht (Lehre W16c-B12). Die
    /// Eintraege sind die ECHTE <see cref="Menuetabelle"/>, nicht ein
    /// zurechtgelegter Baum: Der Befund haengt an ihren drei Ebenen.
    /// </summary>
    private IRenderedComponent<Menueband> AusHuelle(Action<Menuepunkt>? gewaehlt = null)
    {
        var gaben = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["Eintraege"] = Menuetabelle.Eintraege,
            ["Bezeichnung"] = "Hauptmenü",
        };

        if (gewaehlt is not null)
            gaben["Gewaehlt"] = EventCallback.Factory.Create(this, gewaehlt);

        return Render<Menueband>(b =>
        {
            b.OpenComponent<Menueband>(0);
            b.AddMultipleAttributes(1, gaben);
            b.CloseComponent();
        });
    }

    /// <summary>
    /// Das Stilblatt der Bibliothek — fuer die Regeln, die kein Markup zeigt.
    /// Die ZEILENENDEN werden angeglichen: Auf dem Windows-Laeufer steht in der
    /// Arbeitskopie CRLF, und ein Suchmuster mit "\n" fiele dort ins Leere
    /// (derselbe Grund wie im Stilblock von <c>StartseiteTests</c>).
    /// </summary>
    private static string Stilblatt()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        return File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"))
                   .Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    [Fact]
    public void Ein_Punkt_der_zweiten_Ebene_klappt_beim_Klick_auf()
    {
        // DER BEFUND, wie ihn der Anwender meldete: "Administration" oeffnet
        // und zeigt seine sieben aufklappenden Eintraege - aber "Waermebedarf
        // & Heizung ▸" tat beim Klick nichts.
        var cut = AusHuelle();

        cut.Find("#menue-Administration").Click();
        Assert.Equal("false", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("#menue-MenuItem_Brauchwasser"));

        cut.Find("#menue-MenuItem_WBundHeizung").Click();

        Assert.Equal("true", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll(".epos-menueband-klappe--tief"));

        // Alle sechs Punkte stehen im DOM - nicht bloss die Klappe.
        foreach (string name in new[]
                 {
                     "MenuItem_Brauchwasser", "MenuItem_Kessel", "MenuItem_Prozesswaerme",
                     "MenuItem_PufferSp", "MenuItem_WaermebedarfExtern", "MenuItem_WP",
                 })
            Assert.Single(cut.FindAll("#menue-" + name));

        // Der Kopf bleibt offen - das Untermenue haengt IN ihm.
        Assert.Equal("true", cut.Find("#menue-Administration").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Ein_zweiter_Klick_klappt_dasselbe_Untermenue_wieder_zu()
    {
        var cut = AusHuelle();

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_WBundHeizung").Click();
        cut.Find("#menue-MenuItem_WBundHeizung").Click();

        Assert.Equal("false", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll(".epos-menueband-klappe--tief"));

        // Nur die zweite Ebene faellt; der Kopf steht weiter offen.
        Assert.Equal("true", cut.Find("#menue-Administration").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Zwei_Untermenues_derselben_Ebene_schliessen_einander_aus()
    {
        // Bis W16c-B13 lag die zweite Ebene in einem flachen HashSet ueber
        // NAMEN - zwei Geschwister konnten gleichzeitig offen stehen. Jetzt
        // traegt jede Ebene des Pfades genau EINEN Eintrag.
        var cut = AusHuelle();

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_WBundHeizung").Click();
        cut.Find("#menue-MenuItem_StromBedarfundSp").Click();

        Assert.Equal("false", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("aria-expanded"));
        Assert.Equal("true", cut.Find("#menue-MenuItem_StromBedarfundSp").GetAttribute("aria-expanded"));

        Assert.Empty(cut.FindAll("#menue-MenuItem_Brauchwasser"));
        Assert.Single(cut.FindAll("#menue-MenuItem_Stromverbraucher"));
        Assert.Single(cut.FindAll(".epos-menueband-klappe--tief"));
    }

    [Fact]
    public void Ein_Kopfwechsel_nimmt_das_ganze_Untermenue_mit()
    {
        var cut = AusHuelle();

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_WBundHeizung").Click();
        cut.Find("#menue-Projekte").Click();

        Assert.Equal("false", cut.Find("#menue-Administration").GetAttribute("aria-expanded"));
        Assert.Equal("true", cut.Find("#menue-Projekte").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll(".epos-menueband-klappe--tief"));
    }

    [Fact]
    public void Ein_Punkt_der_dritten_Ebene_meldet_und_schliesst_das_ganze_Band()
    {
        // Der EINZIGE Weg des Bestands ueber drei Ebenen: Administration ▸
        // Energiesysteme ▸ Photovoltaik ▸ "Bearbeiten...".
        Menuepunkt? gemeldet = null;
        var cut = AusHuelle(m => gemeldet = m);

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_Energiesysteme").Click();
        cut.Find("#menue-MenuItem_PV").Click();

        Assert.Equal("true", cut.Find("#menue-MenuItem_PV").GetAttribute("aria-expanded"));
        Assert.Equal(2, cut.FindAll(".epos-menueband-klappe--tief").Count);

        cut.Find("#menue-MenuItem_PC_Bearbeiten").Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(Seitenschluessel.PvAdmin, gemeldet!.Ziel);
        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));
    }

    [Fact]
    public void Das_offene_Menue_traegt_eine_Schliessflaeche_und_der_Klick_darauf_schliesst()
    {
        // Der Ersatz fuer das gestrichene @onfocusout: ein durchsichtiger
        // Deckel ueber der Ansicht. Er faengt den Klick NEBEN das Menue - auf
        // Maus wie auf dem Finger, und ohne Fokus.
        var cut = AusHuelle();
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));

        cut.Find("#menue-Administration").Click();
        Assert.Single(cut.FindAll(".epos-menueband-schliessflaeche"));

        cut.Find(".epos-menueband-schliessflaeche").Click();

        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));
    }

    [Fact]
    public void Ein_Fokuswechsel_im_Band_schliesst_nichts_mehr()
    {
        // DIE URSACHE des Befundes W16c-B13: focusout blast nach oben und
        // feuerte auch dann, wenn der Fokus INNERHALB des Bandes wanderte -
        // der Zeigerdruck auf eine Untermenuezeile nahm dem Kopfknopf den
        // Fokus, das Band raeumte die Klappe weg, und der Klick fand seine
        // Zeile nicht mehr vor. Am Band haengt jetzt kein focusout mehr;
        // bunit meldet das als fehlenden Handler.
        var cut = AusHuelle();
        cut.Find("#menue-Administration").Click();

        Assert.Throws<MissingEventHandlerException>(
            () => cut.Find(".epos-menueband").FocusOut());

        Assert.Single(cut.FindAll(".epos-menueband-klappe"));
    }

    [Fact]
    public void Der_Tastaturweg_oeffnet_und_schliesst_ein_Untermenue_der_zweiten_Ebene()
    {
        var cut = AusHuelle();
        var band = cut.Find(".epos-menueband");

        // Vom ersten Kopf nach rechts auf "Administration", dann ↓ hinein.
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.Equal("true", cut.Find("#menue-Administration").GetAttribute("aria-expanded"));

        // Der Zeiger steht auf dem ersten Punkt der Klappe - nur er ist
        // tabulierbar (roving tabindex, dieselbe Regel wie im Baustein Reiter).
        Assert.Equal("0", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("tabindex"));
        Assert.Equal("-1", cut.Find("#menue-MenuItem_StromBedarfundSp").GetAttribute("tabindex"));

        // → klappt SEIN Untermenue auf und stellt den Zeiger auf dessen ersten Punkt.
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("true", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("aria-expanded"));
        Assert.Single(cut.FindAll("#menue-MenuItem_Brauchwasser"));
        Assert.Equal("0", cut.Find("#menue-MenuItem_Brauchwasser").GetAttribute("tabindex"));

        // ↓ wandert darin weiter.
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("0", cut.Find("#menue-MenuItem_Kessel").GetAttribute("tabindex"));
        Assert.Equal("-1", cut.Find("#menue-MenuItem_Brauchwasser").GetAttribute("tabindex"));

        // ← schliesst NUR diese Ebene; der Zeiger steht danach auf ihrem Kopf.
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("false", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("#menue-MenuItem_Brauchwasser"));
        Assert.Equal("0", cut.Find("#menue-MenuItem_WBundHeizung").GetAttribute("tabindex"));
        Assert.Equal("true", cut.Find("#menue-Administration").GetAttribute("aria-expanded"));

        // Esc schliesst alles - auch die Schliessflaeche.
        band.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));
    }

    [Fact]
    public void Der_Tastaturweg_reicht_bis_in_die_dritte_Ebene()
    {
        var cut = AusHuelle();
        var band = cut.Find(".epos-menueband");

        band.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // Administration
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });    // hinein
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });    // Strombedarf
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });    // Energiesysteme
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // auf
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // Photovoltaik auf

        Assert.Equal("true", cut.Find("#menue-MenuItem_Energiesysteme").GetAttribute("aria-expanded"));
        Assert.Equal("true", cut.Find("#menue-MenuItem_PV").GetAttribute("aria-expanded"));
        Assert.Equal("0", cut.Find("#menue-MenuItem_PC_Bearbeiten").GetAttribute("tabindex"));

        // Zweimal ← fuehrt Ebene um Ebene zurueck, nicht auf einen Schlag hinaus.
        band.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("false", cut.Find("#menue-MenuItem_PV").GetAttribute("aria-expanded"));
        Assert.Equal("true", cut.Find("#menue-MenuItem_Energiesysteme").GetAttribute("aria-expanded"));

        band.KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("false", cut.Find("#menue-MenuItem_Energiesysteme").GetAttribute("aria-expanded"));
        Assert.Equal("true", cut.Find("#menue-Administration").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Der_Tabulator_fuehrt_aus_dem_Band_und_schliesst_es()
    {
        var cut = AusHuelle();

        cut.Find("#menue-Administration").Click();
        cut.Find("#menue-MenuItem_WBundHeizung").Click();

        cut.Find(".epos-menueband").KeyDown(new KeyboardEventArgs { Key = "Tab" });

        Assert.Empty(cut.FindAll(".epos-menueband-klappe"));
        Assert.Empty(cut.FindAll(".epos-menueband-schliessflaeche"));
    }

    [Fact]
    public void Die_Schliessflaeche_liegt_im_Stilblatt_unter_dem_Band()
    {
        // Eine bunit-Probe sieht nur die Klasse (Lehre W6-B-1). Dass der Deckel
        // die ganze Ansicht abdeckt UND das Band darueber bleibt, steht allein
        // im Stilblatt - stimmten die drei z-Ebenen nicht, faenge der Deckel den
        // Klick auf den Menuepunkt selbst ab, und der Befund waere zurueck.
        string css = Stilblatt();

        int a = css.IndexOf(".epos-menueband-schliessflaeche {", StringComparison.Ordinal);
        Assert.True(a >= 0, "Die Regel .epos-menueband-schliessflaeche fehlt im Stilblatt");
        string deckel = css.Substring(a, css.IndexOf('}', a) - a);

        Assert.Contains("position: fixed", deckel, StringComparison.Ordinal);
        Assert.Contains("inset: 0", deckel, StringComparison.Ordinal);
        Assert.Contains("z-index: 39", deckel, StringComparison.Ordinal);

        // Das Band traegt seinen eigenen Stapelkontext ueber dem Deckel; die
        // Klappe (40) steht darin und also ebenfalls darueber. Gesucht wird ab
        // dem Anker des Befundes, damit die Regel des Bandes von weiter oben
        // (Flexkasten, Farbe, Rahmen) nicht mitgezaehlt wird.
        int anker = css.IndexOf("DIE SCHLIESSFLAECHE (Befund W16c-B13", StringComparison.Ordinal);
        Assert.True(anker >= 0, "Der Block zum Befund W16c-B13 fehlt im Stilblatt");

        int b = css.IndexOf(".epos-menueband {", anker, StringComparison.Ordinal);
        Assert.True(b >= 0, "Das Band traegt keinen eigenen Stapelkontext");
        string band = css.Substring(b, css.IndexOf('}', b) - b);

        Assert.Contains("position: relative", band, StringComparison.Ordinal);
        Assert.Contains("z-index: 41", band, StringComparison.Ordinal);
    }
}
