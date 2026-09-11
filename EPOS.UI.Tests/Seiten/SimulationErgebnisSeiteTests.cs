using System.Globalization;
using Bunit;
using EPOS.UI.Dienste;
using EPOS.UI.Dialoge.Strom;
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
public class SimulationErgebnisSeiteTests : EposBunitContext
{
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
    }

    // =====================================================================
    // Probendaten — ein Projekt mit Wärmepumpe, Heizkessel und Speicher
    // =====================================================================

    private SimulationErgebnisDaten _daten = Voll();

    private static SimulationErgebnisDaten Voll() => new SimulationErgebnisDaten
    {
        IdProjekt = 1030,
        ErgebnisGueltig = true,
        Parameter = new ParameterDaten(),
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

    /// <summary>
    /// Startet den Lauf. Seit #216 gibt es dafür keinen Knopf MEHR AUF DIESER
    /// SEITE — der Weg ist <c>LaufStarten()</c>, und genau den nimmt Schritt ② der
    /// Ansicht.
    /// </summary>
    private static void Starten(IRenderedComponent<SimulationErgebnisSeite> seite)
        => seite.InvokeAsync(() => seite.Instance.LaufStarten());

    // =====================================================================
    // Die Reiterleiste
    // =====================================================================

    /// <summary>
    /// NEUN Blätter, nicht zehn: Der Reiter „Parameter" ist mit Auftrag #216
    /// gefallen (Windows-Abnahme 11.09.2026, Punkt 3) — seine Felder stehen in
    /// Schritt ① der Ansicht.
    /// </summary>
    [Fact]
    public void Die_Leiste_zeigt_nur_die_Blaetter_der_gewaehlten_Erzeuger()
    {
        var seite = Zeichnen();
        var knoepfe = seite.FindAll("div.epos-simerg > fieldset > div.epos-reiter > div.epos-reiter-leiste button[role='tab']");

        // Übersicht, Bedarf, Wärmepumpe, Heizkessel, Stromspeicher, Ergebnis
        Assert.Equal(6, knoepfe.Count);
        Assert.DoesNotContain(knoepfe, k => k.TextContent == "BHKW");
        Assert.DoesNotContain(knoepfe, k => k.TextContent == "Photovoltaik");
    }

    /// <summary>
    /// DER REITER „PARAMETER" IST WEG (Windows-Abnahme #216, Punkt 3: „Nimm
    /// Parameter heraus"). Weder ein Blatt noch sein Schlüssel noch seine
    /// Beschriftung stehen noch da.
    /// </summary>
    [Fact]
    public void Es_gibt_keinen_Reiter_Parameter_mehr()
    {
        var seite = Zeichnen();

        Assert.DoesNotContain("reiter-PARAMETER", seite.Markup);
        Assert.DoesNotContain(
            seite.FindAll("div.epos-simerg > fieldset > div.epos-reiter > div.epos-reiter-leiste button[role='tab']"),
            k => k.TextContent == "Parameter");
    }

    /// <summary>
    /// „Stelle die Übersicht als erstes dar" (Windows-Abnahme #216, Punkt 4). Bis
    /// dahin stand der Reiter „Parameter" vorn.
    /// </summary>
    [Fact]
    public void Der_Startreiter_ist_die_Uebersicht()
    {
        var seite = Zeichnen();
        Assert.Equal("UEBERSICHT", seite.Instance.AktivesBlatt);
    }

    [Fact]
    public void Ohne_Erzeuger_bleiben_drei_Blaetter()
    {
        _daten = new SimulationErgebnisDaten { ErgebnisGueltig = true };
        var seite = Zeichnen();

        Assert.Equal(3, seite.FindAll("div.epos-simerg > fieldset > div.epos-reiter > div.epos-reiter-leiste button[role='tab']").Count);
    }

    // =====================================================================
    // #216: keine Fussleiste, kein zweites i/KI-Paar
    // =====================================================================

    /// <summary>
    /// DIE FUSSLEISTE IST GEFALLEN (Windows-Abnahme #216, Punkt 2): Ihre zwei
    /// Knöpfe „Simulation starten ▶" und „Ergebnis speichern" standen seit #207
    /// ZWEIMAL — hier und in der Ablaufleiste darüber. Beide trägt jetzt die
    /// Werkzeugleiste der <c>SimulationSeite</c>; mit dem eigenen Kopf fällt auch
    /// das zweite Paar [i] [KI].
    /// </summary>
    [Fact]
    public void Die_Seite_traegt_weder_Fussleiste_noch_eigenen_Infoknopf()
    {
        var seite = Zeichnen();

        Assert.Empty(seite.FindAll("div.epos-simerg-fuss"));
        Assert.Empty(seite.FindAll("div.epos-simerg-kopf"));
        Assert.Empty(seite.FindAll("button.epos-infoknopf"));
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
        Starten(seite);

        Assert.True(seite.Instance.Laeuft);
        Assert.Single(seite.FindAll("[role='progressbar']"));

        // Waehrend des Laufs darf „Ergebnis speichern" der Werkzeugleiste nicht.
        Assert.False(seite.Instance.SpeichernMoeglich);

        seite.InvokeAsync(() => _laufFertig!.SetResult(Rueckmeldung.Still));
        seite.WaitForState(() => !seite.Instance.Laeuft);
    }

    /// <summary>Die Phasenmeldung des Kerns erreicht den Balken.</summary>
    [Fact]
    public void Die_Phasenmeldung_erreicht_den_Balken()
    {
        var seite = Zeichnen();
        Starten(seite);

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
        Starten(seite);

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
        Starten(seite);

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

        Assert.False(seite.Instance.SpeichernMoeglich);
        Assert.Equal(0, _gespeichert);
    }

    [Fact]
    public void Speichern_meldet_sein_Ergebnis()
    {
        var seite = Zeichnen();
        Assert.True(seite.Instance.SpeichernMoeglich);

        seite.InvokeAsync(() => seite.Instance.ErgebnisSpeichern());

        Assert.Equal(1, _gespeichert);
        seite.WaitForAssertion(() => Assert.Contains("gespeichert", seite.Markup));
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

        // Der Startreiter ist seit #216 die „Uebersicht" - sie holt ihre drei Bilder.
        int nachDemAufbau = _auftraege.Count;
        Assert.True(nachDemAufbau >= 1);

        seite.Find("button[role='tab'][id='reiter-BEDARF']").Click();
        int nachErstemBetreten = _auftraege.Count;
        Assert.True(nachErstemBetreten > nachDemAufbau);

        // Zurück und wieder hin: derselbe Schlüssel, kein neuer Auftrag an die Hülle.
        seite.Find("button[role='tab'][id='reiter-UEBERSICHT']").Click();
        seite.Find("button[role='tab'][id='reiter-BEDARF']").Click();

        Assert.Equal(nachErstemBetreten, _auftraege.Count);
    }

    // =====================================================================
    //  W11b-B-28: die Speicherparameter im Ergebnisreiter "Stromspeicher"
    // =====================================================================

    /// <summary>
    /// Die Seite reicht <c>Parameter.Speicher</c>, die Dienste, den Sperrzustand und
    /// den Optimierungsknopf an den Reiter „Stromspeicher" durch — dort sitzt seit
    /// dem Anwenderwunsch vom 10.09.2026 der Parameterblock.
    /// </summary>
    [Fact]
    public void Die_Speicherparameter_gehen_an_den_Stromspeicherreiter()
    {
        _daten = Voll();
        _daten.Parameter.Speicher = new SpeicherParameterDaten
        {
            VarianteVorhanden = true,
            Variantenstatus = "Aktive Variante: Speicher 1",
            SoCMinProzent = 10,
            SoCMaxProzent = 90
        };

        var seite = Zeichnen();
        seite.Find("button[role='tab'][id='reiter-STROMSPEICHER']").Click();

        var reiter = seite.FindComponent<StromspeicherReiter>().Instance;
        Assert.Same(_daten.Parameter.Speicher, reiter.Parameter);
        Assert.NotNull(reiter.Dienste);

        // Der Block arbeitet seit W11b-B-29 auf der UEBERGABE selbst - was die Seite
        // durchreicht, ist der Stand, den er zeigt und schreibt.
        var block = seite.FindComponent<SpeicherParameterBlock>().Instance;
        Assert.Same(_daten.Parameter.Speicher, block.Daten);
        Assert.Equal("", block.Meldung);
    }

    /// <summary>
    /// PAKET P3 (#192): Der Knopf zieht KEINE Überlagerung mehr auf, sondern WECHSELT
    /// die Ansicht (Muster W16c‑E‑3). Die Seite meldet das über <c>AuslegungOeffnen</c>
    /// an ihre Hülle; die kennt den gerechneten Simulationslauf und den Weg zur Wurzel.
    /// </summary>
    [Fact]
    public void Der_Auslegungsknopf_wechselt_die_Ansicht_statt_eine_Ueberlagerung_zu_oeffnen()
    {
        int gewechselt = 0;
        var dienste = Dienste();
        dienste.OptimierungVorgaben = () => new SpeicherOptimierungVorgaben();
        dienste.AuslegungOeffnen = () => gewechselt++;

        var seite = Zeichnen(dienste: dienste);
        seite.Find("button[role='tab'][id='reiter-STROMSPEICHER']").Click();
        seite.FindAll("button").Single(b => b.TextContent.Trim() ==
            WindowsFormsApplication1.MyResource.Resource.OPT_BTN_OEFFNEN).Click();

        Assert.Equal(1, gewechselt);

        // Die Ergebnisseite traegt die Auslegung nicht mehr in sich.
        Assert.Empty(seite.FindComponents<EPOS.UI.Seiten.Strom.StromspeicherAuslegungSeite>());
    }

    /// <summary>Ohne Datenseite zeichnet die Seite eine leere Ergebnisansicht.</summary>
    [Fact]
    public void Ohne_Dienste_bleibt_die_Seite_leer_aber_bedienbar()
    {
        var seite = Render<SimulationErgebnisSeite>(p => p
            .Add(x => x.Automatikstart, false));

        Assert.Equal(3, seite.FindAll("div.epos-simerg > fieldset > div.epos-reiter > div.epos-reiter-leiste button[role='tab']").Count);

        // Ohne Bilddelegat wird kein Bild angefordert; der Baustein zeigt seinen
        // Platzhalter.
        Assert.Empty(_auftraege);
    }
}
