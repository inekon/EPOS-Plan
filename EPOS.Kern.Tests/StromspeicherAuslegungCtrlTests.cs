using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests;

/// <summary>
/// Die DATENSEITE der Ansicht „Stromspeicher-Auslegung"
/// (<see cref="StromspeicherAuslegungCtrl"/>, Paket P3 / Auftrag #192) gegen die
/// Testdatenbank.
///
/// <para><b>Warum hier und nicht im Referenzlauf.</b> Der Referenzlauf rechnet einen
/// BESTEHENDEN Projektstand nach; er aktiviert keine Projektflotte und speichert keinen
/// Arbeitsstand. Genau das tun aber die Wege, die mit P3 aus der Windows-Hülle in den
/// Kern gezogen sind — und ihr Ausgang entscheidet, ob die Flotte im nächsten
/// Projektlauf überhaupt fährt.</para>
///
/// <para>Gearbeitet wird auf <b>Projekt 1046 „Prüfprojekt Speicherflotte"</b>, dem
/// einzigen Projekt der Testdatenbank mit einem aktivierten Stand
/// <c>@Projektflotte</c> (Anwenderentscheid SP‑O‑8). <b>Geschrieben wird
/// ausschließlich in der Arbeitskopie</b> — <see cref="TestDatenbank"/> biegt
/// <c>DataRepository.PfadUeberschreibung</c> darauf um, die Vergleichsbasis bleibt
/// unberührt.</para>
/// </summary>
[Collection("Testdatenbank")]
public sealed class StromspeicherAuslegungCtrlTests
{
    /// <summary>Das Prüfprojekt der Speicherflotte (Basis R7, SP‑O‑8).</summary>
    private const int Pruefprojekt = 1046;

    // =====================================================================
    //  Vorbelegung und Lauf
    // =====================================================================

    /// <summary>
    /// Ohne Simulationslauf gibt es keine Bezugsspitze — und die Vorbelegung sagt es,
    /// statt eine zu erfinden.
    /// </summary>
    [Fact]
    public void Ohne_Lauf_ist_keine_Bezugsspitze_da_und_die_Vorbelegung_steht_trotzdem()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);

        Assert.False(ctrl.LaufVorhanden);
        Assert.Equal(0.0, ctrl.BezugsspitzeKw());

        SpeicherOptimierungVorgaben vorgaben = ctrl.Vorgaben();
        Assert.NotNull(vorgaben);
        Assert.NotNull(vorgaben.Eingaben.Auslegung);
        Assert.NotNull(vorgaben.Eingaben.Auslegung!.Flotte);
    }

    /// <summary>
    /// Der Lauf der ERGEBNISSEITE wird hereingereicht, nicht nachgerechnet: Die
    /// EPOS-Zeitreihen stammen aus genau einem Lauf, und zwei nebeneinander wären zwei
    /// Wahrheiten.
    /// </summary>
    [Fact]
    public void Ein_hereingereichter_Lauf_traegt_die_Bezugsspitze()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var sim = new SimulationControl
        {
            simulation_Strombedarf = new SimulationStrombedarf
            {
                Strombedarf_viertelStundenwerte = new[] { 10.0, 42.5, 7.25 }
            }
        };

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        ctrl.LaufUebernehmen(sim);

        Assert.True(ctrl.LaufVorhanden);
        Assert.Equal(42.5, ctrl.BezugsspitzeKw(), 10);
        Assert.Same(sim, ctrl.Lauf);
    }

    // =====================================================================
    //  Rechnen (der Flottenpfad des Pruefprojekts)
    // =====================================================================

    /// <summary>
    /// Der Flottenlauf des Prüfprojekts liefert ein Ergebnis MIT Diagnose — sie ist die
    /// Grundlage des Diagnosebanners (Konzept 2.2 Punkt 2).
    /// </summary>
    [Fact]
    public void Der_Flottenlauf_liefert_ein_Ergebnis_mit_Diagnose()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        SpeicherOptimierungEingaben eingaben = Dateistand(ctrl);

        StromspeicherOptimierungVorbereitung vorbereitung = ctrl.FlotteVorbereiten(eingaben, out string meldung);
        Assert.True(vorbereitung is not null, "Die Vorbereitung ist gescheitert: " + meldung);

        SpeicherFlottenErgebnis ergebnis = ctrl.FlotteRechnen(vorbereitung!, null, CancellationToken.None);

        Assert.True(ergebnis.Erfolg, ergebnis.Meldung);
        Assert.NotNull(ergebnis.Studie);
        Assert.NotNull(ergebnis.Studie!.Variante.Diagnose);
        Assert.Equal(Intervalle, ergebnis.Studie.Variante.Diagnose.IntervalleGesamt);

        // Die Flotte ARBEITET: Sie laedt und entlaedt im Rechenzeitraum.
        Assert.False(ergebnis.Studie.Variante.Diagnose.Arbeitslos);
        Assert.True(ergebnis.Studie.Variante.Diagnose.EntladeenergieAcKWh > 0);
    }

    /// <summary>
    /// Die VORPRÜFUNG läuft ohne Diagnose — sie sagt vor dem Lauf, was ihn entwertet.
    /// Ein unerreichbares Peak-Ziel ist ihr Hauptfall (Befund SP‑O‑10).
    /// </summary>
    [Fact]
    public void Die_Vorpruefung_meldet_ein_unerreichbares_Peak_Ziel()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        SpeicherOptimierungEingaben eingaben = Dateistand(ctrl);
        eingaben.Auslegung!.Flotte!.Optionen.NetzladungErlaubt = false;
        eingaben.Auslegung.Flotte.Optionen.WirtschaftlicherPeakZielwertKw = 0.001;

        IReadOnlyList<FlottenHinweis> hinweise = ctrl.Vorpruefen(eingaben);

        Assert.Contains(hinweise, h => h.Kennung == FlottenHinweisKennung.PeakZielUnterTagesminimum);
        Assert.All(hinweise, h => Assert.False(string.IsNullOrWhiteSpace(h.Text)));
    }

    /// <summary>
    /// Der VORSCHLAG für das Peak-Ziel kommt aus der Referenzzeitreihe, nicht aus einer
    /// festen Zahl — die 50 kW des Bestands sind mit P1 gefallen.
    /// </summary>
    [Fact]
    public void Der_Peak_Ziel_Vorschlag_kommt_aus_der_Reihe()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        FlottenPeakZielVorschlag vorschlag = ctrl.PeakZielVorschlag(Dateistand(ctrl));

        Assert.True(vorschlag.AusReihe, "Der Vorschlag stammt nicht aus der Zeitreihe.");
        Assert.True(vorschlag.ReferenzspitzeKw > 0);
        Assert.True(vorschlag.PeakZielKw > 0);
        Assert.Contains("H₀", vorschlag.Herleitung, StringComparison.Ordinal);
    }

    /// <summary>
    /// „Peak-Ziel bestimmen" findet ein Ziel, das die Flotte auch hält — und meldet,
    /// wie viele Jahresläufe es gekostet hat.
    /// </summary>
    [Fact]
    public void Peak_Ziel_bestimmen_findet_ein_haltbares_Ziel()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        SpeicherOptimierungEingaben eingaben = Dateistand(ctrl);
        eingaben.Auslegung!.Flotte!.Optionen.Betriebsziel = FlottenBetriebsziel.PeakShaving;

        StromspeicherOptimierungVorbereitung vorbereitung = ctrl.FlotteVorbereiten(eingaben, out string meldung);
        Assert.True(vorbereitung is not null, "Die Vorbereitung ist gescheitert: " + meldung);

        FlottenPeakZielErgebnis ergebnis =
            ctrl.PeakZielBestimmen(vorbereitung!, null, CancellationToken.None);

        Assert.True(ergebnis.Laeufe > 0 && ergebnis.Laeufe <= FlottenPeakZiel.HoechsteLaeufe);
        Assert.True(ergebnis.PeakZielKw > 0);
        Assert.True(ergebnis.PeakZielKw <= ergebnis.ReferenzspitzeKw);
        Assert.False(string.IsNullOrWhiteSpace(ergebnis.Herleitung));
    }

    // =====================================================================
    //  Aktivieren und Deaktivieren
    // =====================================================================

    /// <summary>
    /// Aktivieren und Deaktivieren gehen über den Controller — und der merkt sich, DASS
    /// geschrieben wurde: Die Ergebnisseite zieht daran ihr gespeichertes Ergebnis nach.
    /// </summary>
    [Fact]
    public void Aktivieren_und_Deaktivieren_schalten_die_Projektflotte()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        Assert.True(ctrl.ProjektflotteAktiv(), "Projekt 1046 führt den aktivierten Stand @Projektflotte (SP-O-8).");
        Assert.False(ctrl.ProjektflotteGeaendert);

        Assert.Equal("", ctrl.ProjektflotteDeaktivieren());
        Assert.False(ctrl.ProjektflotteAktiv());
        Assert.True(ctrl.ProjektflotteGeaendert);

        // Ein DATEISTAND schaltet sie nicht wieder an: Der Projektlauf darf seinen
        // Strombedarf nicht still gegen eine fremde Lastdatei tauschen
        // (SpeicherFlottenProjektCtrl.PruefeProjektquellen). Der Controller gibt den
        // Grund zurueck, statt zu werfen.
        SpeicherOptimierungEingaben datei = Dateistand(ctrl);
        StromspeicherOptimierungVorbereitung vDatei = ctrl.FlotteVorbereiten(datei, out string meldungDatei);
        Assert.True(vDatei is not null, "Die Vorbereitung ist gescheitert: " + meldungDatei);
        SpeicherFlottenErgebnis ausDatei = ctrl.FlotteRechnen(vDatei!, null, CancellationToken.None);
        Assert.True(ausDatei.Erfolg, ausDatei.Meldung);

        Assert.NotEqual("", ctrl.ProjektflotteAktivieren(ausDatei));
        Assert.False(ctrl.ProjektflotteAktiv());

        // Wieder an: ueber den EIGENEN Simulationslauf und dessen EPOS-Zeitreihen —
        // genau der Weg, den die Ansicht ohne hereingereichten Lauf geht (Muster iU9-W11a).
        string fehler = ctrl.SimulationslaufVorbereiten(
            new SimulationWaermebedarf(), new SimulationStrombedarf(), out SimulationControl lauf);
        Assert.True(fehler == null, "Der Simulationslauf liess sich nicht vorbereiten: " + fehler);
        Assert.Null(ctrl.SimulationslaufRechnen(lauf, null, CancellationToken.None));
        ctrl.LaufUebernehmen(lauf);
        Assert.True(ctrl.BezugsspitzeKw() > 0);

        SpeicherOptimierungEingaben eingaben = Eposstand(ctrl);
        StromspeicherOptimierungVorbereitung vorbereitung = ctrl.FlotteVorbereiten(eingaben, out string meldung);
        Assert.True(vorbereitung is not null, "Die Vorbereitung ist gescheitert: " + meldung);

        SpeicherFlottenErgebnis ergebnis = ctrl.FlotteRechnen(vorbereitung!, null, CancellationToken.None);
        Assert.True(ergebnis.Erfolg, ergebnis.Meldung);

        Assert.Equal("", ctrl.ProjektflotteAktivieren(ergebnis));
        Assert.True(ctrl.ProjektflotteAktiv());
    }

    /// <summary>Ein Profilname mit <c>@</c> ist für interne Stände reserviert.</summary>
    [Fact]
    public void Ein_Profilname_mit_Klammeraffe_wird_abgewiesen()
    {
        using var testDb = new TestDatenbank();
        Assert.True(testDb.Vorhanden, "Die Testdatenbank ist für diesen Integrationstest erforderlich.");

        var ctrl = new StromspeicherAuslegungCtrl(Pruefprojekt);
        Assert.Throws<ArgumentException>(
            () => ctrl.ProfilSpeichern(new SpeicherOptimierungEingaben { Auslegung = new() }, "@Projektflotte"));
    }

    // =====================================================================
    //  Hilfen
    // =====================================================================

    /// <summary>Die Zahl der Viertelstunden eines Normaljahres.</summary>
    private const int Intervalle = 35040;

    /// <summary>
    /// Derselbe Arbeitsstand, aber aus den EPOS-Zeitreihen des hereingereichten Laufs.
    /// Nur so darf ein Ergebnis die Projektflotte aktivieren
    /// (<c>SpeicherFlottenProjektCtrl.PruefeProjektquellen</c>).
    /// </summary>
    private static SpeicherOptimierungEingaben Eposstand(StromspeicherAuslegungCtrl ctrl)
    {
        SpeicherOptimierungEingaben eingaben = Grundstand(ctrl);
        SpeicherAuslegungKonfiguration a = eingaben.Auslegung!;
        a.Lastquelle = SpeicherAuslegungQuelle.Epos;
        a.PvQuelle = SpeicherAuslegungQuelle.Epos;
        a.Preisquelle = SpeicherAuslegungQuelle.Epos;
        a.LastDatei = null;
        a.PvDatei = null;
        a.PreisDatei = null;
        a.FlottenProjektjahre = new List<FlottenProjektjahr>();
        return eingaben;
    }

    /// <summary>
    /// Ein Arbeitsstand, der OHNE Simulationslauf rechnen kann: Die Flotte kommt aus
    /// dem gespeicherten Projektflottenstand, die Zeitreihen aus einer synthetischen
    /// Datei. So braucht der Prüffall keinen Jahreslauf des ganzen Projekts.
    /// </summary>
    private static SpeicherOptimierungEingaben Dateistand(StromspeicherAuslegungCtrl ctrl)
    {
        SpeicherOptimierungEingaben eingaben = Grundstand(ctrl);
        SpeicherAuslegungKonfiguration a = eingaben.Auslegung!;

        // Last UND Bezugspreis kommen aus der DATEI — nur so rechnet der Stand ohne
        // einen vorangegangenen Jahreslauf des Projekts. Bei der Preisquelle ist
        // „Keine" nicht erlaubt (SpeicherAuslegungCtrl.PruefeQuellen lässt EPOS, Datei
        // oder Preisprofil zu), deshalb steht dort eine flache Preisreihe.
        a.Lastquelle = SpeicherAuslegungQuelle.Datei;
        a.PvQuelle = SpeicherAuslegungQuelle.Keine;
        a.Preisquelle = SpeicherAuslegungQuelle.Datei;
        a.LastDatei = Jahresreihe(SpeicherZeitreihenRolle.Last, "last.csv", Lastreihe());
        a.PreisDatei = Jahresreihe(SpeicherZeitreihenRolle.Bezug, "preis.csv", Preisreihe());
        return eingaben;
    }

    /// <summary>
    /// Der gemeinsame Unterbau beider Arbeitsstände: die Flotte aus dem gespeicherten
    /// Projektflottenstand, ein einziges Referenzjahr, keine Größensuche und die
    /// Kostensätze aus dem Dialog. Ohne sie bricht die Vorbereitung ab (Befund #185) —
    /// die Einheiten des Prüfprojekts tragen keine eigenen Kosten.
    /// </summary>
    private static SpeicherOptimierungEingaben Grundstand(StromspeicherAuslegungCtrl ctrl)
    {
        SpeicherOptimierungEingaben eingaben = ctrl.Vorgaben().Eingaben.Kopie();
        SpeicherAuslegungKonfiguration a = eingaben.Auslegung ??= new SpeicherAuslegungKonfiguration();

        a.FlottenGroessenOptimieren = false;
        a.FlotteImProjektAktiv = false;
        a.Investitionsquelle = SpeicherKostenQuelle.Dialog;
        a.Betriebsquelle = SpeicherKostenQuelle.Dialog;
        a.DirekteKosten = new SpeicherKostensaetze
        {
            InvestEurProKwh = 300.0,
            InvestEurProKw = 200.0,
            InvestVorhanden = true,
            BetriebEurProKwJahr = 5.0,
            BetriebEurProKwhJahr = 0.0,
            BetriebEurProKwhEntladen = 0.0,
            BetriebVorhanden = true
        };
        a.VerwendeteKosten = new SpeicherKostensaetze();

        FlottenStudieKonfiguration flotte = a.Flotte ??= new FlottenStudieKonfiguration();
        flotte.Auslegung.Achsen.Clear();
        flotte.Optionen.EnergieAusgleichEuroProKWh ??= 0.0;
        flotte.Wirtschaftlichkeit.ProjektjahreBeiWiederholung = 1;
        flotte.Wirtschaftlichkeit.ReferenzjahrExplizitWiederholen = true;
        return eingaben;
    }

    /// <summary>
    /// Eine Dateireihe auf der Achse des Normaljahres 2026. Es ist genau die Achse, die
    /// auch die Flottenrechnung erwartet: ein vollständiges Kalenderjahr in
    /// Europe/Berlin, durchgehend im Viertelstundenraster
    /// (<c>SpeicherAuslegungCtrl.PruefeJahresachse</c> und
    /// <c>SpeicherFlottenStudieCtrl.PruefeGanzesJahr</c>) — deshalb kommt sie aus
    /// <see cref="SpeicherFlottenStudieCtrl.ModellZeitachse"/> selbst.
    /// </summary>
    private static SpeicherZeitreihe Jahresreihe(
        SpeicherZeitreihenRolle rolle, string name, double[] werte)
    {
        DateTimeOffset[] zeit = SpeicherFlottenStudieCtrl.ModellZeitachse(2026, werte.Length);
        return new SpeicherZeitreihe
        {
            QuelleName = name,
            Rolle = rolle,
            ZeitstempelUtc = zeit,
            Werte = werte,
            Optionen = new SpeicherZeitreihenOptionen { Rolle = rolle, ZeitzoneId = "Europe/Berlin" }
        };
    }

    /// <summary>
    /// Ein flacher Bezugspreis in EUR/kWh — der Rechenweg rechnet die Dateireihe selbst
    /// in ct/kWh um.
    /// </summary>
    private static double[] Preisreihe() => Enumerable.Repeat(0.30, Intervalle).ToArray();

    /// <summary>
    /// Eine Tageszackenreihe über ein Jahr: Grundlast 20 kW, mittags 60 kW. Sie hat eine
    /// Spitze zu kappen und ein Tagesminimum, unter das die Last wirklich fällt.
    /// </summary>
    private static double[] Lastreihe()
    {
        double[] werte = new double[Intervalle];
        for (int i = 0; i < werte.Length; i++)
        {
            int viertelImTag = i % 96;
            werte[i] = viertelImTag is >= 44 and < 52 ? 60.0 : 20.0;
        }
        return werte;
    }
}
