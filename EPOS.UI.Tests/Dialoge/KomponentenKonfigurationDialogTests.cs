using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der KomponentenKonfigurationDialog (Anwenderwunsch 16.09.2026, Screenshots
/// „Simulation → Konfiguration"): „Erstelle dort einen Knopf anstelle des blauen
/// Balkens ‚Parameter für die Simulation' mit dem Konfigurationsdialog. Dies soll
/// für alle Komponenten so erfolgen … Nimm auch die bestehenden Parameter (z. B.
/// Wärmepumpe ‚mit Heizstab (falls vorhanden)', Heizkessel ‚Betriebsbereitschaft')
/// in den Konfigurationsdialog."
///
/// <para>FELDBESTAND je Art: Wärmepumpe = Anlagenkonfiguration (soweit vorhanden)
/// plus die projektweite Heizstab-Einstellung, Heizkessel = Betriebsbereitschaft,
/// BHKW = Betriebsart und untere Leistungsgrenze. Dazu der Rückweg OK/Abbrechen und
/// der Fall ohne Gaben.</para>
/// </summary>
public class KomponentenKonfigurationDialogTests : EposBunitContext
{
    private readonly List<bool> _ergebnis = new();

    private ParameterDaten _werte = new ParameterDaten
    {
        Betriebsart = 1,
        UntersteLeistungsgrenze = 30,
        Heizstab = true,
        Bereitschaft = 8000
    };

    private IRenderedComponent<KomponentenKonfigurationDialog> Zeige(
        Komponentenart art, WaermepumpeAnlageDaten? anlage = null, bool titel = false)
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        return Render<KomponentenKonfigurationDialog>(p =>
        {
            p.Add(x => x.Komponente, art);
            p.Add(x => x.Werte, _werte);
            p.Add(x => x.TitelAnzeigen, titel);
            p.Add(x => x.Bezeichner, "BHKW · Modul 1");
            if (anlage is not null) p.Add(x => x.Anlage, anlage);
            p.Add(x => x.Geschlossen, (bool ok) => _ergebnis.Add(ok));
        });
    }

    /// <summary>Ein Knopf der Schlussleiste: 0 = Abbrechen, 1 = OK.</summary>
    private static IElement Leiste(IRenderedComponent<KomponentenKonfigurationDialog> cut,
                                   int platz)
        => cut.FindAll("div.epos-leiste button")[platz];

    // ================================================================== Feldbestand

    /// <summary>
    /// Das BHKW: die Optionsgruppe der drei Betriebsarten, das Ganzzahlfeld der
    /// unteren Leistungsgrenze samt seiner Herleitung und der Erklärkasten —
    /// dieselben Ressourcen, die bis zum 16.09.2026 an der Karte standen.
    /// </summary>
    [Fact]
    public void Das_BHKW_zeigt_Betriebsart_und_untere_Leistungsgrenze()
    {
        var cut = Zeige(Komponentenart.Bhkw);

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_GRP_BETRIEBSART,
                        cut.Markup);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_LBL_UNTERE_LEISTUNGSGRENZE,
                        cut.Markup);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_INFO_BHKW,
                        cut.Markup);
        Assert.Equal(3, cut.FindAll("input[type=radio]").Count);

        // „stromgefuehrt" ist vorgewaehlt (Steuerwert 1).
        Assert.True(cut.FindAll("input[type=radio]")[1].HasAttribute("checked"));

        // Der Dialog aendert NUR die Arbeitskopie - geschrieben wird beim Wirt.
        cut.FindAll("input[type=radio]")[2].Change(true);
        Assert.Equal(2, _werte.Betriebsart);
    }

    /// <summary>Der Heizkessel: ein Zahlenfeld [h/a], mehr nicht.</summary>
    [Fact]
    public void Der_Heizkessel_zeigt_die_Betriebsbereitschaft()
    {
        var cut = Zeige(Komponentenart.Heizkessel);

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_LBL_BEREITSCHAFT,
                        cut.Markup);
        Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.SIMERG_GRP_BETRIEBSART,
                              cut.Markup);

        IElement feld = cut.FindAll("input")[0];
        Assert.Equal("8000", feld.GetAttribute("value"));

        feld.Input("7500");
        Assert.Equal(7500.0, _werte.Bereitschaft);
    }

    /// <summary>
    /// Die Wärmepumpe OHNE Anlagendaten: allein die PROJEKTEINSTELLUNG „mit Heizstab
    /// (falls vorhanden)" — und ihre Herleitungszeile sagt, dass sie projektweit
    /// gilt.
    ///
    /// <para><b>Befund 16.09.2026:</b> Der Rechenweg liest ausschließlich
    /// <c>Tab_Einstellungen.WP_Heizstab</c> (<c>SimulationControl</c> →
    /// <c>SimulationWaermepumpe.Mit_Heizstab</c> → <c>Heizstabphase</c>). Deshalb
    /// steht dieser Schalter in JEDEM Wärmepumpendialog, auch ohne Naht zur
    /// Anlage.</para>
    /// </summary>
    [Fact]
    public void Die_Waermepumpe_zeigt_ohne_Anlage_nur_die_Projekteinstellung()
    {
        var cut = Zeige(Komponentenart.Waermepumpe);

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_CHK_HEIZSTAB,
                        cut.Markup);
        Assert.Contains("Projekteinstellung", cut.Markup);
        Assert.Empty(cut.FindComponents<WaermepumpeKonfiguration>());

        // Die zweite Herleitungszeile trennt die zwei gleichnamigen Schalter - ohne
        // Anlagenkonfiguration gibt es nichts zu trennen.
        Assert.DoesNotContain("Elektrische Nachheizung aktivieren", cut.Markup);

        cut.Find("input[type=checkbox]").Change(false);
        Assert.False(_werte.Heizstab);
    }

    /// <summary>
    /// Mit Anlagendaten steht die Konfiguration DIESER Anlage darüber — dieselbe
    /// Komponente, die auch der Wärmepumpen-Anlagendialog zeigt.
    /// </summary>
    [Fact]
    public void Die_Waermepumpe_zeigt_mit_Anlage_ihre_Konfiguration()
    {
        var anlage = new WaermepumpeAnlageDaten { Bezeichner = "WP 1", Heizstab = false };

        var cut = Zeige(Komponentenart.Waermepumpe, anlage);

        Assert.NotNull(cut.FindComponent<WaermepumpeKonfiguration>());

        // Beide Schalter stehen da - und die Herleitung sagt, welcher was tut.
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.SIMERG_CHK_HEIZSTAB,
                        cut.Markup);
        Assert.Contains("Elektrische Nachheizung", cut.Markup);
    }

    /// <summary>
    /// OHNE GABEN zeichnet der Dialog: keine Art, keine Werte, kein Rückruf — die
    /// Schlussleiste steht, der Inhalt ist leer.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Dialog()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

        var cut = Render<KomponentenKonfigurationDialog>();

        Assert.NotNull(cut.Find("div.epos-dialog"));
        Assert.Equal(2, cut.FindAll("div.epos-leiste button").Count);
        Assert.Empty(cut.FindAll("input"));
    }

    // ================================================================== Rückweg

    /// <summary>OK meldet <c>true</c>, Abbrechen <c>false</c> — der Wirt schreibt.</summary>
    [Fact]
    public void OK_meldet_true_und_Abbrechen_meldet_false()
    {
        var cut = Zeige(Komponentenart.Bhkw);

        Leiste(cut, 1).Click();
        Assert.Equal(new[] { true }, _ergebnis);

        Leiste(cut, 0).Click();
        Assert.Equal(new[] { true, false }, _ergebnis);
    }

    /// <summary>Esc wirkt wie Abbrechen; Enter bleibt unbelegt (der Wirt schreibt).</summary>
    [Fact]
    public void Esc_bricht_ab_und_Enter_ist_unbelegt()
    {
        var cut = Zeige(Komponentenart.Heizkessel);

        cut.Find("div.epos-dialog").KeyDown("Enter");
        Assert.Empty(_ergebnis);

        cut.Find("div.epos-dialog").KeyDown("Escape");
        Assert.Equal(new[] { false }, _ergebnis);
    }

    // ================================================================== Titel

    /// <summary>
    /// EIN TITEL, EINE STELLE: In der Überlagerung der Seite trägt DIE den Titel —
    /// der Dialog zeichnet dann weder Überschrift noch Schließkreuz
    /// (Anwenderbefund 15.09.2026: „Doppeltes Kreuz dürfen nicht sein!").
    /// </summary>
    [Fact]
    public void Ohne_TitelAnzeigen_bleiben_Ueberschrift_und_Kreuz_weg()
    {
        var cut = Zeige(Komponentenart.Bhkw);

        Assert.Empty(cut.FindAll("h1.epos-dialog-titel"));
        Assert.Empty(cut.FindAll("button.epos-dialog-zu"));
        Assert.NotNull(cut.Find("div.epos-dialog-kopf--ohnetitel"));
    }

    /// <summary>Für sich allein trägt er beides — Titel aus Vorlage und Bezeichner.</summary>
    [Fact]
    public void Mit_TitelAnzeigen_steht_der_Kartentitel_im_Kopf()
    {
        var cut = Zeige(Komponentenart.Bhkw, titel: true);

        Assert.Equal("Konfiguration · BHKW · Modul 1",
                     cut.Find("h1.epos-dialog-titel").TextContent);
        Assert.NotNull(cut.Find("button.epos-dialog-zu"));
    }
}
