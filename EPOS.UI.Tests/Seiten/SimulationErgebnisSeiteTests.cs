using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// Die ERGEBNISSEITE der Simulation (iU9-W11b.13), Vorbild
/// <c>Form_Simulation_Detail</c> (7 629 Z. + 3 082 Designer) samt fünf
/// Nebenmasken.
///
/// <para>Soll: der Startreiter, die Blätter nach <c>Tool_1..6</c>, der Lauf mit
/// Fortschritt und Endlage „Übersicht", der Abbruch, die Laufmeldungen,
/// „Ergebnis speichern" nur nach einem vollständigen Lauf, der Sperrzustand,
/// der Zwischenspeicher der Bilder und die Überlagerungen.</para>
/// </summary>
public class SimulationErgebnisSeiteTests : BunitContext
{
    private readonly CultureInfo _kulturVorher = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _zahlenVorher = CultureInfo.CurrentCulture;

    private readonly List<Bildauftrag> _auftraege = new();
    private int _laeufe;
    private int _abbrueche;
    private int _gespeichert;
    private Action<double?, string>? _melder;
    private TaskCompletionSource<Rueckmeldung>? _laufFertig;

    public SimulationErgebnisSeiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
    }

    protected override void Dispose(bool disposing)
    {
        CultureInfo.CurrentUICulture = _kulturVorher;
        CultureInfo.CurrentCulture = _zahlenVorher;
        base.Dispose(disposing);
    }

    // =====================================================================
    // Probendaten — ein Projekt mit Wärmepumpe, Heizkessel und Speicher
    // =====================================================================

    private SimulationErgebnisDaten _daten = Voll();

    private static SimulationErgebnisDaten Voll() => new SimulationErgebnisDaten
    {
        IdProjekt = 1030,
        ErgebnisGueltig = true,
        Parameter = new ParameterDaten
        {
            Unterblaetter = new[] { ParameterBlatt.Bedarf, ParameterBlatt.Waermepumpe }
        },
        ReiterWaermepumpe = true,
        ReiterHeizkessel = true,
        ReiterStromspeicher = true,
        Kennzahlen = new SimulationErgebnisCtrl.UebersichtKennzahlen
        {
            WaermebedarfGesamtMwh = 480.25,
            StrombedarfGesamtMwh = 120.5
        },
        Uebersicht = new UebersichtDaten
        {
            Waermepumpe = true,
            Heizkessel = true,
            WaermebedarfVorhanden = true,
            StrombedarfVorhanden = true
        },
        Bedarf = new BedarfDaten
        {
            KanalMwh = new[] { 400.0, 80.0, 0.0 },
            Kanalnamen = new[] { "Heizung", "Brauchwasser", "Prozesswärme" },
            KanalDa = new[] { true, true, false }
        },
        Speicher = new SpeicherErgebnisDaten { Kopf = "Noch keine Speicherrechnung" }
    };

    private SimulationErgebnisDienste Dienste(bool mitLauf = true, bool mitAbbruch = true)
        => new SimulationErgebnisDienste
        {
            Laden = _ => _daten,
            Bild = a => { _auftraege.Add(a); return new byte[] { 1 }; },
            Laufen = mitLauf
                ? melder =>
                {
                    _laeufe++;
                    _melder = melder;
                    _laufFertig = new TaskCompletionSource<Rueckmeldung>();
                    return _laufFertig.Task;
                }
                : null,
            Abbrechen = mitAbbruch ? () => _abbrueche++ : null,
            Speichern = () => { _gespeichert++; return new Rueckmeldung(true, "gespeichert"); }
        };

    private IRenderedComponent<SimulationErgebnisSeite> Zeichnen(bool automatik = false,
                                                                 SimulationErgebnisDienste? dienste = null)
        => Render<SimulationErgebnisSeite>(p => p
            .Add(x => x.Dienste, dienste ?? Dienste())
            .Add(x => x.StartProjekt, 1030)
            .Add(x => x.Automatikstart, automatik));

    // =====================================================================
    // Die Reiterleiste
    // =====================================================================

    /// <summary>
    /// Zehn Blätter, nicht elf: Der Behälter-Reiter „Simulation" (R3) war die
    /// Menüliste und entfällt mit ihr (Befund W11-B11).
    /// </summary>
    [Fact]
    public void Die_Leiste_zeigt_nur_die_Blaetter_der_gewaehlten_Erzeuger()
    {
        var seite = Zeichnen();
        var knoepfe = seite.FindAll("div.epos-simerg > fieldset > div.epos-reiter > div.epos-reiter-leiste button[role='tab']");

        // Parameter, Übersicht, Bedarf, Wärmepumpe, Heizkessel, Stromspeicher, Ergebnis
        Assert.Equal(7, knoepfe.Count);
        Assert.DoesNotContain(knoepfe, k => k.TextContent == "BHKW");
        Assert.DoesNotContain(knoepfe, k => k.TextContent == "Photovoltaik");
    }

    /// <summary>Der Startreiter ist „Parameter" (wörtlich :415-421).</summary>
    [Fact]
    public void Der_Startreiter_ist_Parameter()
    {
        var seite = Zeichnen();
        Assert.Equal("PARAMETER", seite.Instance.AktivesBlatt);
    }

    [Fact]
    public void Ohne_Erzeuger_bleiben_vier_Blaetter()
    {
        _daten = new SimulationErgebnisDaten { ErgebnisGueltig = true };
        var seite = Zeichnen();

        Assert.Equal(4, seite.FindAll("div.epos-simerg > fieldset > div.epos-reiter > div.epos-reiter-leiste button[role='tab']").Count);
    }

    // =====================================================================
    // Der Lauf
    // =====================================================================

    /// <summary>
    /// Der AUTOMATIKSTART (Befund W11-B48) bleibt wörtlich — und die sichtbare
    /// ENDLAGE ist wie bisher die „Übersicht".
    /// </summary>
    [Fact]
    public void Der_Automatikstart_laeuft_und_endet_auf_der_Uebersicht()
    {
        var seite = Zeichnen(automatik: true);

        Assert.Equal(1, _laeufe);
        Assert.True(seite.Instance.Laeuft);

        seite.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        seite.WaitForState(() => !seite.Instance.Laeuft);

        Assert.Equal("UEBERSICHT", seite.Instance.AktivesBlatt);
    }

    /// <summary>Ohne Automatikstart läuft nichts von selbst.</summary>
    [Fact]
    public void Ohne_Automatikstart_laeuft_nichts()
    {
        Zeichnen();
        Assert.Equal(0, _laeufe);
    }

    /// <summary>Während des Laufs steht der Fortschritt da und der Knopf ist gesperrt.</summary>
    [Fact]
    public void Waehrend_des_Laufs_steht_der_Fortschritt()
    {
        var seite = Zeichnen();
        seite.FindAll("div.epos-simerg-fuss button")[0].Click();

        Assert.True(seite.Instance.Laeuft);
        Assert.Single(seite.FindAll("[role='progressbar']"));
        Assert.True(seite.FindAll("div.epos-simerg-fuss button")[0].HasAttribute("disabled"));

        seite.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        seite.WaitForState(() => !seite.Instance.Laeuft);
    }

    /// <summary>Die Phasenmeldung des Kerns erreicht den Balken.</summary>
    [Fact]
    public void Die_Phasenmeldung_erreicht_den_Balken()
    {
        var seite = Zeichnen();
        seite.FindAll("div.epos-simerg-fuss button")[0].Click();

        seite.InvokeAsync(() => _melder!(0.6, "Photovoltaik"));
        seite.WaitForAssertion(() => Assert.Contains("Photovoltaik", seite.Markup));

        seite.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        seite.WaitForState(() => !seite.Instance.Laeuft);
    }

    /// <summary>Der Abbrechen-Knopf steht nur mit Delegat und meldet seinen Klick.</summary>
    [Fact]
    public void Der_Abbruch_meldet_sich()
    {
        var seite = Zeichnen();
        seite.FindAll("div.epos-simerg-fuss button")[0].Click();

        seite.Find("[role='progressbar']");
        var abbrechen = seite.FindAll("button");
        foreach (var b in abbrechen)
            if (b.TextContent.Contains("Abbrechen")) { b.Click(); break; }

        Assert.Equal(1, _abbrueche);

        seite.InvokeAsync(() => _laufFertig!.SetResult(new Rueckmeldung(false, "")));
        seite.WaitForState(() => !seite.Instance.Laeuft);
    }

    /// <summary>Ein abgebrochener Lauf meldet seinen Grund als Banner.</summary>
    [Fact]
    public void Ein_abgebrochener_Lauf_meldet_seinen_Grund()
    {
        var seite = Zeichnen();
        seite.FindAll("div.epos-simerg-fuss button")[0].Click();

        seite.InvokeAsync(() => _laufFertig!.SetResult(
            new Rueckmeldung(false, "Simulation abgebrochen: keine Klimaregion")));
        seite.WaitForState(() => !seite.Instance.Laeuft);

        Assert.Contains("keine Klimaregion", seite.Markup);
    }

    // =====================================================================
    // Ergebnis speichern und Sperrzustand
    // =====================================================================

    /// <summary>
    /// „Ergebnis speichern" nur nach einem VOLLSTÄNDIGEN Lauf — die
    /// Zustandsmaschine aus Nacharbeit Paket 8, Befund N1.
    /// </summary>
    [Fact]
    public void Speichern_ist_ohne_gueltiges_Ergebnis_gesperrt()
    {
        _daten = Voll();
        _daten.ErgebnisGueltig = false;

        var seite = Zeichnen();
        var knoepfe = seite.FindAll("div.epos-simerg-fuss button");

        Assert.True(knoepfe[1].HasAttribute("disabled"));   // „Ergebnis speichern"
        Assert.Equal(0, _gespeichert);
    }

    [Fact]
    public void Speichern_meldet_sein_Ergebnis()
    {
        var seite = Zeichnen();
        foreach (var b in seite.FindAll("div.epos-simerg-fuss button"))
            if (b.TextContent.Contains("Ergebnis speichern")) { b.Click(); break; }

        Assert.Equal(1, _gespeichert);
        Assert.Contains("gespeichert", seite.Markup);
    }

    /// <summary>
    /// Der Sperrzustand (Schemamigration, ADR-001): Grund als Banner, alles
    /// gesperrt — „Beenden" muss trotzdem gehen.
    /// </summary>
    [Fact]
    public void Der_Sperrzustand_meldet_und_sperrt()
    {
        _daten = Voll();
        _daten.Gesperrt = true;
        _daten.Sperrgrund = "Die Datenbank ist nicht auf dem benötigten Stand.";

        var seite = Zeichnen();

        Assert.Contains("benötigten Stand", seite.Markup);
        Assert.True(seite.Find("fieldset").HasAttribute("disabled"));
        Assert.True(seite.FindAll("div.epos-simerg-fuss button")[0].HasAttribute("disabled"));
    }

    // =====================================================================
    // Laufmeldungen und Bilder
    // =====================================================================

    /// <summary>
    /// Die Laufmeldungen als anklickbares Banner; der Volltext steht in einer
    /// Überlagerung (der Vorläufer zeigte ihn als MessageBox).
    /// </summary>
    [Fact]
    public void Die_Laufmeldungen_oeffnen_ihren_Volltext()
    {
        _daten = Voll();
        _daten.LaufmeldungenAnzahl = 3;
        _daten.Laufmeldungen = "Hinweis 1\nHinweis 2\nHinweis 3";

        var seite = Zeichnen();
        Assert.Contains("3 Hinweise zum Lauf", seite.Markup);

        foreach (var b in seite.FindAll("button"))
            if (b.TextContent.Contains("Hinweise zum Lauf")) { b.Click(); break; }

        Assert.Single(seite.FindAll("[role='dialog']"));
        Assert.Contains("Hinweis 2", seite.Markup);
    }

    /// <summary>
    /// Bilder entstehen erst beim BETRETEN eines Reiters und werden je
    /// Schalterstellung zwischengespeichert — zwölf PNG je Lauf im Voraus wären
    /// zu teuer (Risiko der Vermessung § 11.5).
    /// </summary>
    [Fact]
    public void Bilder_entstehen_erst_beim_Betreten_und_bleiben_zwischengespeichert()
    {
        var seite = Zeichnen();

        // Der Startreiter ist „Parameter" - er hat kein Bild.
        Assert.Empty(_auftraege);

        seite.Find("button[role='tab'][id='reiter-BEDARF']").Click();
        int nachErstemBetreten = _auftraege.Count;
        Assert.True(nachErstemBetreten >= 2);

        // Zurück und wieder hin: derselbe Schlüssel, kein neuer Auftrag an die Hülle.
        seite.Find("button[role='tab'][id='reiter-PARAMETER']").Click();
        seite.Find("button[role='tab'][id='reiter-BEDARF']").Click();

        Assert.Equal(nachErstemBetreten, _auftraege.Count);
    }

    /// <summary>Ohne Datenseite zeichnet die Seite eine leere Ergebnisansicht.</summary>
    [Fact]
    public void Ohne_Dienste_bleibt_die_Seite_leer_aber_bedienbar()
    {
        var seite = Render<SimulationErgebnisSeite>(p => p
            .Add(x => x.Automatikstart, false));

        Assert.Equal(4, seite.FindAll("div.epos-simerg > fieldset > div.epos-reiter > div.epos-reiter-leiste button[role='tab']").Count);

        // Ohne Bilddelegat wird kein Bild angefordert; der Baustein zeigt seinen
        // Platzhalter.
        Assert.Empty(_auftraege);
    }
}
