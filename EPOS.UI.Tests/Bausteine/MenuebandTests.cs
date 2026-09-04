using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// NACHWEIS N4 (Vermessung § 11.8) — das Menueband des Hauptfensters.
///
/// <para>Geprueft wird genau das, was der Rueckbau von <c>MDIMainForm</c>
/// aufgibt: die VOLLZAEHLIGKEIT der Punkte, ihre BESCHRIFTUNG in beiden
/// Sprachen und die Zusicherung, dass jeder Klick einen
/// <see cref="Seitenschluessel"/> meldet — und nichts sonst. Fiele einer der
/// 54 Punkte beim Umzug aus, saehe man es an keiner anderen Stelle mehr; der
/// Designer, der ihn bisher belegte, ist mit W16c.3 geloescht.</para>
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
        // Programms hatte immer 54 Punkte (Befund W16c-B2).
        Assert.Equal(54, Punkte.Count);

        // Sechs Trenner standen im Designer, zwei haengten BaueVariantenMenue
        // und InitKiHilfe programmatisch ein.
        Assert.Equal(8, Menuetabelle.Alle.Count(p => p.Trenner));

        // Fuenf Punkte der obersten Ebene: Projekt, Administration, Hilfe,
        // Deutsch, Englisch (menuToolbar.Items.AddRange).
        Assert.Equal(5, Menuetabelle.Eintraege.Count);
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
        // 42 der 54 Punkte handeln, 12 klappen nur auf (Projekt,
        // Administration, die sieben Untermenues der Administration, PV,
        // Solarkollektoren und Hilfe). Der Vorlaeufer fuehrte dafuer 34
        // Designer-Handler und neun Lambdas in den Init*-Methoden - die
        // Differenz von einem ist MenuItem_PV_Import_PAN, dessen Handler zu
        // KEINEM Steuerelement gehoerte (Befund W16-B24).
        Assert.Equal(12, Punkte.Count(p => p.Klappt));
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
    public void Das_Band_zeigt_die_fuenf_Punkte_der_obersten_Ebene()
    {
        var cut = Render<Menueband>(p => p.Add(x => x.Eintraege, Menuetabelle.Eintraege));

        var knoepfe = cut.FindAll(".epos-menueband > .epos-menueband-punkt > .epos-menueband-knopf, " +
                                  ".epos-menueband > .epos-menueband-knopf");
        Assert.Equal(5, knoepfe.Count);
        Assert.Equal("Projekt", knoepfe[0].TextContent.Trim());
        Assert.Equal("Administration", knoepfe[1].TextContent.Trim());
        Assert.Equal("Hilfe", knoepfe[2].TextContent.Trim());
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
    public void Ein_Sprachpunkt_der_obersten_Ebene_meldet_unmittelbar()
    {
        Menuepunkt? gemeldet = null;
        var cut = Render<Menueband>(p => p
            .Add(x => x.Eintraege, Menuetabelle.Eintraege)
            .Add(x => x.Gewaehlt, (Menuepunkt m) => gemeldet = m));

        cut.Find("#menue-Englisch").Click();

        Assert.NotNull(gemeldet);
        Assert.Equal(Seitenschluessel.SpracheEnglisch, gemeldet!.Ziel);
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
        var mitBild = Menuetabelle.Alle.Where(p => p.Bild.Length > 0)
                                       .ToDictionary(p => p.Name, p => p.Bild, StringComparer.Ordinal);

        Assert.Equal(11, mitBild.Count);
        Assert.Equal("Menu1", mitBild["MenuItem_WBundHeizung"]);
        Assert.Equal("germany", mitBild["Deutsch"]);
        Assert.Equal("usa", mitBild["Englisch"]);
        Assert.Equal("lizenzen_32", mitBild["MenuItem_LizenzVerwaltung"]);
    }
}
