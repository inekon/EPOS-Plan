using System.Globalization;
using System.Reflection;
using System.Threading;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Start;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// <b>Die Sinnbilder der 21 Startkacheln</b> — Anwenderwunsch vom 05.09.2026
/// („Icons fehlen"), Entscheid <b>W16b‑E‑3</b>.
///
/// <para>Geprüft wird dreierlei: dass die Tabelle <see cref="Kachelbilder"/>
/// JEDEN der 21 Kachelschlüssel führt, dass die genannte DATEI wirklich unter
/// <c>EPOS.UI/wwwroot/bilder/start/</c> liegt (dieselbe Dateisystemprobe wie die
/// Stilblattwache in <c>KostenSeiteTests</c> — ein Tippfehler im Dateinamen wäre
/// sonst erst beim Anwender als leere Kachel zu sehen), und dass die fünf
/// Reiterkomponenten das Bild auch wirklich zeichnen.</para>
///
/// <para>Die Sprache ist auf de-DE gepinnt (Hausregel seit iU9-W8): Der
/// Windows-Läufer läuft mit englischer Oberfläche, und die Erwartungswerte
/// unten sind deutsche Beschriftungen.</para>
/// </summary>
public class KachelbilderTests : BunitContext
{
    public KachelbilderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

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

    // =====================================================================
    //  Die Tabelle
    // =====================================================================

    /// <summary>Die 21 Schlüssel aus <see cref="Kachelschluessel"/> selbst.</summary>
    private static IReadOnlyList<string> AlleSchluessel()
    {
        List<string> schluessel = typeof(Kachelschluessel)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.Equal(Kachelschluessel.Anzahl, schluessel.Count);
        return schluessel;
    }

    [Fact]
    public void Jede_der_21_Kacheln_traegt_ein_Bild()
    {
        IReadOnlyList<string> schluessel = AlleSchluessel();

        Assert.Equal(Kachelschluessel.Anzahl, Kachelbilder.Alle.Count);

        foreach (string s in schluessel)
        {
            Assert.True(Kachelbilder.Alle.ContainsKey(s), $"Kachel {s} ohne Sinnbild");
            Assert.StartsWith("_content/EPOS.UI/bilder/start/", Kachelbilder.Quelle(s));
            Assert.NotEqual("", Kachelbilder.Klasse(s));
        }
    }

    [Fact]
    public void Jede_genannte_Datei_liegt_unter_wwwroot_bilder_start()
    {
        // Dieselbe Dateisystemprobe wie die Stilblattwache aus W5-B-1: Ein
        // Dateiname, den es nicht gibt, faellt sonst erst beim Anwender auf -
        // ein <img> ohne Datei zeigt nichts und meldet nichts.
        string ordner = Bilderordner();

        foreach ((string Datei, string Klasse) eintrag in Kachelbilder.Alle.Values)
        {
            string pfad = Path.Combine(ordner, eintrag.Datei);
            Assert.True(File.Exists(pfad), $"Bilddatei fehlt: {eintrag.Datei}");
        }
    }

    [Fact]
    public void Die_fuenf_Aktionskarten_tragen_ihr_Symbol()
    {
        // Herkunft: karte_*.KartenBild aus dem eingefrorenen Designer
        // (Pruefmuster/Hauptformular/Form_Start.Designer.cs:238-273). Die sechste
        // Karte "Projekt Details" ist mit E-7 entfallen.
        Assert.Equal("PProjektNeu_Symbol.png", Datei(Kachelschluessel.ProjektNeu));
        Assert.Equal("PProjektOeffnen_Symbol.png", Datei(Kachelschluessel.ProjektOeffnen));
        Assert.Equal("PProjektZuletzt_Symbol.png", Datei(Kachelschluessel.ProjektZuletzt));
        Assert.Equal("PProjektBearbeiten_Symbol.png", Datei(Kachelschluessel.ProjektSpeichernUnter));
        Assert.Equal("PDelete_Symbol.png", Datei(Kachelschluessel.ProjektLoeschen));

        // Sie sind fertig zugeschnitten und brauchen kein CSS-Fenster.
        foreach (string s in new[]
        {
            Kachelschluessel.ProjektNeu, Kachelschluessel.ProjektOeffnen,
            Kachelschluessel.ProjektZuletzt, Kachelschluessel.ProjektSpeichernUnter,
            Kachelschluessel.ProjektLoeschen
        })
        {
            Assert.Equal(Kachelbilder.KLASSE_SYMBOL, Kachelbilder.Klasse(s));
        }
    }

    [Fact]
    public void Die_sechzehn_Bildkacheln_tragen_das_Bild_ihrer_pictureBox()
    {
        // Herkunft: pBox_*.BackgroundImage aus dem eingefrorenen Designer.
        Assert.Equal("PGebaeude.jpg", Datei(Kachelschluessel.Gebaeude));
        Assert.Equal("Unbenannt2.jpg", Datei(Kachelschluessel.WaermebedarfDaten));
        Assert.Equal("Unbenannt3.jpg", Datei(Kachelschluessel.Prozesswaerme));
        Assert.Equal("Unbenannt3.jpg", Datei(Kachelschluessel.Brauchwasser));

        Assert.Equal("PStdLastProfil.jpg", Datei(Kachelschluessel.StromStandardprofil));
        Assert.Equal("PStromProfilEigenes.jpg", Datei(Kachelschluessel.StromEigenesProfil));
        Assert.Equal("PStromMessdaten.jpg", Datei(Kachelschluessel.StromMessdaten));

        Assert.Equal("PWP.jpg", Datei(Kachelschluessel.Waermepumpe));
        Assert.Equal("PHeizkessel.jpg", Datei(Kachelschluessel.Heizkessel));
        Assert.Equal("PProjektSolarthermie.jpg", Datei(Kachelschluessel.Solarthermie));
        Assert.Equal("PBHKW.jpg", Datei(Kachelschluessel.Bhkw));
        Assert.Equal("PProjektPV.jpg", Datei(Kachelschluessel.Photovoltaik));
        Assert.Equal("PSSpeicher.jpg", Datei(Kachelschluessel.Stromspeicher));
        Assert.Equal("PPufferSpeicher.jpg", Datei(Kachelschluessel.Pufferspeicher));

        Assert.Equal("PSchnellSim.jpg", Datei(Kachelschluessel.SimulationKonfiguration));
        Assert.Equal("PDetailSim.jpg", Datei(Kachelschluessel.SimulationErgebnis));
    }

    [Fact]
    public void Die_zwei_flachen_Kacheln_tragen_das_eigene_Fenster()
    {
        // pBox_Stromspeicher und pBox_Pufferspeicher standen im Bestand HALBHOCH
        // nebeneinander (405 x 112 statt 404 x 185); ihre JPG sind 554 x 117,
        // und das Sinnbild sitzt darin 22 px hoeher.
        Assert.Equal(Kachelbilder.KLASSE_AUSSCHNITT_FLACH,
                     Kachelbilder.Klasse(Kachelschluessel.Stromspeicher));
        Assert.Equal(Kachelbilder.KLASSE_AUSSCHNITT_FLACH,
                     Kachelbilder.Klasse(Kachelschluessel.Pufferspeicher));

        // Alle uebrigen JPG-Kacheln nehmen das hohe Fenster.
        Assert.Equal(Kachelbilder.KLASSE_AUSSCHNITT, Kachelbilder.Klasse(Kachelschluessel.Gebaeude));
        Assert.Equal(Kachelbilder.KLASSE_AUSSCHNITT, Kachelbilder.Klasse(Kachelschluessel.Heizkessel));
    }

    [Fact]
    public void Die_drei_Fensterregeln_stehen_im_Stilblatt()
    {
        // Das Fenster ist CSS, nicht Markup - eine bunit-Probe saehe den Fehler
        // nie. Deshalb liest dieser Fall das Stilblatt selbst (Muster W5-B-1).
        string hoch = Stilblock(".epos-kachel-bild--ausschnitt {");
        Assert.Contains("object-fit: none", hoch);
        Assert.Contains("object-position: -40px -40px", hoch);

        string flach = Stilblock(".epos-kachel-bild--ausschnitt-flach {");
        Assert.Contains("object-fit: none", flach);
        Assert.Contains("object-position: -36px -18px", flach);

        // Das Symbol ist schon zugeschnitten - es bekommt nur eine Hoehe.
        string symbol = Stilblock(".epos-kachel-bild--symbol {");
        Assert.Contains("height: 84px", symbol);
        Assert.DoesNotContain("object-position", symbol);
    }

    // =====================================================================
    //  Was die Reiter zeichnen
    // =====================================================================

    [Fact]
    public void Der_Reiter_Projekt_zeichnet_fuenf_Symbole()
    {
        var cut = Render<ProjektReiter>(p => p
            .Add(x => x.Kacheln, Kacheln(Reiterschluessel.Projekt,
                                         Kachelschluessel.ProjektNeu,
                                         Kachelschluessel.ProjektOeffnen,
                                         Kachelschluessel.ProjektZuletzt,
                                         Kachelschluessel.ProjektSpeichernUnter,
                                         Kachelschluessel.ProjektLoeschen)));

        var bilder = cut.FindAll(".epos-kachel img");
        Assert.Equal(5, bilder.Count);
        Assert.All(bilder, b => Assert.Contains(Kachelbilder.KLASSE_SYMBOL, b.ClassName));
        Assert.All(bilder, b => Assert.Equal("", b.GetAttribute("alt")));
        Assert.Equal("_content/EPOS.UI/bilder/start/PProjektNeu_Symbol.png",
                     bilder[0].GetAttribute("src"));
    }

    [Fact]
    public void Der_Reiter_Energieerzeuger_zeichnet_sieben_Ausschnitte()
    {
        var cut = Render<ErzeugerReiter>(p => p
            .Add(x => x.Kacheln, Kacheln(Reiterschluessel.Erzeuger,
                                         Kachelschluessel.Waermepumpe,
                                         Kachelschluessel.Heizkessel,
                                         Kachelschluessel.Solarthermie,
                                         Kachelschluessel.Bhkw,
                                         Kachelschluessel.Photovoltaik,
                                         Kachelschluessel.Stromspeicher,
                                         Kachelschluessel.Pufferspeicher)));

        var bilder = cut.FindAll(".epos-kachel img");
        Assert.Equal(7, bilder.Count);
        Assert.Equal("_content/EPOS.UI/bilder/start/PWP.jpg", bilder[0].GetAttribute("src"));

        // Die zwei flachen stehen an fuenfter und sechster Stelle.
        Assert.Contains(Kachelbilder.KLASSE_AUSSCHNITT_FLACH, bilder[5].ClassName);
        Assert.Contains(Kachelbilder.KLASSE_AUSSCHNITT_FLACH, bilder[6].ClassName);
        Assert.Contains(Kachelbilder.KLASSE_AUSSCHNITT, bilder[0].ClassName);
    }

    [Fact]
    public void Der_Konfigurationsknopf_traegt_sein_Sinnbild()
    {
        // btn_SimKonfig war im Bestand ein Knopf OHNE Bild (Abweichung, W16b-E-3);
        // er ist die 21. Kachel und bekommt deshalb eines.
        var cut = Render<SimulationReiter>(p => p
            .Add(x => x.Kacheln, Kacheln(Reiterschluessel.Simulation,
                                         Kachelschluessel.SimulationErgebnis)));

        IElement bild = cut.Find(".epos-startreiter-leiste .epos-knopf img");

        Assert.Equal("_content/EPOS.UI/bilder/start/PSchnellSim.jpg", bild.GetAttribute("src"));
        Assert.Contains("epos-kachel-bild--knopf", bild.ClassName);
        Assert.Equal("", bild.GetAttribute("alt"));
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    private static string Datei(string schluessel) => Kachelbilder.Alle[schluessel].Datei;

    private static IReadOnlyList<StartKachel> Kacheln(string reiter, params string[] schluessel)
        => schluessel
            .Select(s => new StartKachel { Schluessel = s, Reiter = reiter, Titel = s })
            .ToList();

    /// <summary>Der Ordner <c>EPOS.UI/wwwroot/bilder/start</c> im Quellbaum.</summary>
    private static string Bilderordner()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null &&
               !Directory.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "bilder", "start")))
            d = d.Parent;

        Assert.NotNull(d);
        return Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "bilder", "start");
    }

    /// <summary>Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c>.</summary>
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
