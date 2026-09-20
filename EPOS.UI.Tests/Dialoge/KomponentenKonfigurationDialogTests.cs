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
        Bereitschaft = 8000
    };

    /// <summary>
    /// Der Hilfedienst wird EINMAL je Prüfstand eingelegt — bunit lässt keine
    /// Dienstregistrierung mehr zu, sobald die erste Komponente gezeichnet ist, und
    /// ein Fall, der zwei Arten nacheinander zeigt, bräuchte sie sonst zweimal.
    /// </summary>
    private bool _hilfeEingelegt;

    private IRenderedComponent<KomponentenKonfigurationDialog> Zeige(
        Komponentenart art, WaermepumpeAnlageDaten? anlage = null, bool titel = false,
        IReadOnlyList<EPOS.UI.Bausteine.EnergietraegerWahl.Eintrag>? traeger = null)
    {
        if (!_hilfeEingelegt)
        {
            Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
            _hilfeEingelegt = true;
        }

        return Render<KomponentenKonfigurationDialog>(p =>
        {
            p.Add(x => x.Komponente, art);
            p.Add(x => x.Werte, _werte);
            p.Add(x => x.TitelAnzeigen, titel);
            p.Add(x => x.Bezeichner, "BHKW · Modul 1");
            if (anlage is not null) p.Add(x => x.Anlage, anlage);
            if (traeger is not null) p.Add(x => x.Traegerkatalog, traeger);
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
    /// Die Wärmepumpe OHNE Anlagendaten zeigt KEINEN Schalter mehr.
    ///
    /// <para><b>Anwenderentscheid 16.09.2026 (Auftrag #299).</b> Bis dahin stand hier
    /// die PROJEKTEINSTELLUNG <c>Tab_Einstellungen.WP_Heizstab</c> — der einzige
    /// Schalter, den der Lauf las. Der Heizstab gehört seither der WÄRMEPUMPE
    /// (<c>Tab_Energieanlagen.Heizstab</c>, gelesen je Modul in
    /// <c>SimulationWaermepumpe.ModuleAufbauen</c>); es gibt keinen projektweiten Wert
    /// mehr, den dieser Dialog ohne Anlagendaten zeigen könnte.</para>
    ///
    /// <para><b>Und er bleibt nicht stumm</b> (16.09.2026): Wo die Plattform die Naht
    /// zur Anlage nicht stellt (iOS, Proben) oder die Kartenzeile keine Anlage führt,
    /// steht eine BENANNTE Meldung statt eines leeren Dialogkörpers — „was eine
    /// Plattform nicht kann, wird benannt abgelehnt, nie still übergangen".</para>
    /// </summary>
    [Fact]
    public void Die_Waermepumpe_meldet_ohne_Anlage_die_fehlende_Naht()
    {
        var cut = Zeige(Komponentenart.Waermepumpe);

        Assert.Empty(cut.FindComponents<WaermepumpeKonfiguration>());
        Assert.Empty(cut.FindAll("input[type=checkbox]"));

        // Kein leerer Koerper: die Meldung IST hier der Inhalt.
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.SIMKONF_MSG_WP_OHNE_ANLAGE,
                     cut.Find(".epos-warnbanner-text").TextContent.Trim());
        Assert.Contains("nicht verfügbar", cut.Markup);

        // Die Schlussleiste steht trotzdem - der Dialog ist offen.
        Assert.Equal(2, cut.FindAll("div.epos-leiste button").Count);
    }

    /// <summary>
    /// Die Meldung steht NUR bei der Wärmepumpe ohne Anlage — Heizkessel und BHKW
    /// führen projektweite Werte und brauchen keine Naht.
    /// </summary>
    [Fact]
    public void Die_Meldung_steht_nur_im_Waermepumpenzweig()
    {
        string meldung = WindowsFormsApplication1.MyResource.Resource.SIMKONF_MSG_WP_OHNE_ANLAGE;

        Assert.DoesNotContain(meldung, Zeige(Komponentenart.Heizkessel).Markup);
        Assert.DoesNotContain(meldung, Zeige(Komponentenart.Bhkw).Markup);

        var mitAnlage = Zeige(Komponentenart.Waermepumpe,
                              new WaermepumpeAnlageDaten { Bezeichner = "WP 1" });
        Assert.DoesNotContain(meldung, mitAnlage.Markup);
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

        // GENAU EIN Heizstabschalter - der der Anlage (Auftrag #299). Der zweite,
        // projektweite ist mit dem Entscheid entfallen, und der verbliebene heisst
        // seither "Heizstab mitrechnen", weil er der ist, den der Lauf liest.
        Assert.Contains("Heizstab mitrechnen", cut.Markup);
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

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F2)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie bindet über die
    /// Sichtklasse <c>KomponentenKonfigurationKiSicht</c> und steht deshalb nicht in
    /// der Markup-Probe des Dialogkatalogs — dieser Fall ist ihr Ersatz: Die Maske
    /// steht gezeichnet da, die Brücke liest die projektweite Leistungsgrenze, und
    /// ein Setzen landet in der Arbeitskopie.
    /// </summary>
    /// <remarks>
    /// <b>Monotone Aussage</b> (Muster <c>KiMaskenhakenTests</c>): Geprüft wird, was
    /// nach dem Zeichnen DA ist — die Brücke ist prozessweiter Zustand.
    /// </remarks>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Leistungsgrenze()
    {
        var cut = Zeige(Komponentenart.Bhkw);

        Assert.True(WindowsFormsApplication1.KiMaskenbruecke.IstAngemeldet(
                        WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION));

        WindowsFormsApplication1.KiFeldzugang zugang =
            WindowsFormsApplication1.KiMaskenbruecke.Feldzugang(
                WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION,
                "bhkw_leistungsgrenze");

        Assert.NotNull(zugang);
        Assert.Equal(30, zugang.Lesen());

        Assert.True(zugang.Setzbar);
        zugang.Setzen(45);
        cut.Render();

        Assert.Equal(45, _werte.UntersteLeistungsgrenze);
    }

    /// <summary>
    /// <b>Dieselben Felder unter einer anderen Maske.</b> Steht die Konfiguration einer
    /// WÄRMEPUMPE offen, liest und setzt der Assistent die sieben Werte der ANLAGE —
    /// dieselben, die unter <c>Form_WP_Anlage</c> stehen. Eine Maske ist, was offen ist.
    /// </summary>
    [Fact]
    public void Bei_der_Waermepumpe_stehen_die_sieben_Werte_der_Anlage()
    {
        var anlage = new WaermepumpeAnlageDaten { Heizstab = false, Abschaltpunkt = -5 };
        var cut = Zeige(Komponentenart.Waermepumpe, anlage);

        WindowsFormsApplication1.KiFeldzugang heizstab =
            WindowsFormsApplication1.KiMaskenbruecke.Feldzugang(
                WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION, "heizstab");

        Assert.Equal(false, heizstab.Lesen());

        heizstab.Setzen(true);
        cut.Render();

        Assert.True(anlage.Heizstab);

        WindowsFormsApplication1.KiFeldzugang bivalenz =
            WindowsFormsApplication1.KiMaskenbruecke.Feldzugang(
                WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION,
                "bivalenztemperatur");
        Assert.Equal(-5.0, bivalenz.Lesen());
    }

    /// <summary>
    /// <b>Der Energieträger ist ein WAHLFELD</b> (KI-F1b): Gesetzt wird über den
    /// NAMEN des Katalogsatzes, in der Arbeitskopie der Anlage steht danach seine Id.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_den_Energietraeger_ueber_seinen_Namen()
    {
        var anlage = new WaermepumpeAnlageDaten { CarrierId = 3 };
        var cut = Zeige(Komponentenart.Waermepumpe, anlage, traeger: new[]
        {
            new EPOS.UI.Bausteine.EnergietraegerWahl.Eintrag(3, "Strom", "Netzstrom"),
            new EPOS.UI.Bausteine.EnergietraegerWahl.Eintrag(9, "Strom", "Ökostrom")
        });

        WindowsFormsApplication1.KiFeldzugang zugang =
            WindowsFormsApplication1.KiMaskenbruecke.Feldzugang(
                WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION,
                "energietraeger");
        Assert.NotNull(zugang);
        Assert.Equal(3, zugang.Lesen());

        WindowsFormsApplication1.KiFeldumsetzung umsetzung =
            WindowsFormsApplication1.KiFeldwandler.Wandle(zugang, "Ökostrom");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal(9, anlage.CarrierId);

        WindowsFormsApplication1.KiFeldwert wert =
            WindowsFormsApplication1.KiMaskenbruecke
                .Lesen(WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION)
                .Single(f => f.Name == "energietraeger");
        Assert.Equal("Ökostrom", wert.Text);
        Assert.Equal("9", wert.Schluessel);
    }

    /// <summary>
    /// Die BHKW-Betriebsart läuft seit KI-F1b als Wahl: Gesetzt wird über den Text
    /// des Optionsfeldes, in der Arbeitskopie steht danach der Steuerwert 0/1/2.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_die_BHKW_Betriebsart_ueber_ihren_Text()
    {
        var cut = Zeige(Komponentenart.Bhkw);

        WindowsFormsApplication1.KiFeldzugang zugang =
            WindowsFormsApplication1.KiMaskenbruecke.Feldzugang(
                WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION,
                "bhkw_betriebsart");
        Assert.NotNull(zugang);

        string waermegefuehrt = WindowsFormsApplication1.MyResource.Resource.SIMERG_OPT_WAERMEGEFUEHRT;

        WindowsFormsApplication1.KiFeldumsetzung umsetzung =
            WindowsFormsApplication1.KiFeldwandler.Wandle(zugang, waermegefuehrt);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal(0, _werte.Betriebsart);

        WindowsFormsApplication1.KiFeldwert wert =
            WindowsFormsApplication1.KiMaskenbruecke
                .Lesen(WindowsFormsApplication1.KiMaskennamen.KOMPONENTENKONFIGURATION)
                .Single(f => f.Name == "bhkw_betriebsart");
        Assert.Equal(waermegefuehrt, wert.Text);
        Assert.Equal("0", wert.Schluessel);
    }
}
