using System.Collections.Generic;
using System.Diagnostics;
using SpeicherEngine;
using Xunit;

namespace SpeicherEngine.Tests;

/// <summary>
/// DIE ZÄHLREGEL RECHNET, SIE BAUT NICHT (Windows-Abnahme 13.09.2026).
///
/// <para><b>Der Befund, den sie beantwortet.</b> Das Bildschirmfoto der Station 4
/// „Optimierung" meldete „123504 Kandidaten im Grobraster, bis zu 19 im Feinraster —
/// zusammen 123523 von höchstens 10000" und wies den Lauf ab. Die Zeile rechnet LIVE,
/// bei jedem getippten Zeichen im Suchraum; eine Zählung, die dafür das Raster
/// aufbauen müsste, hätte die Oberfläche bei jeder Taste angehalten (bei einer
/// Schrittweite von 1 kWh sind es über 1,2 Millionen Punkte).</para>
///
/// <para><b>Was hier geprüft wird:</b> die Zahlen des Bildschirmfotos Stück für Stück
/// (welche Stützstellen die Engine zählt und warum es 249 und nicht 248 sind), dass die
/// Zählung eine ARITHMETIK ist — sie liefert auch für ein Raster, das sich niemals
/// aufbauen ließe, sofort eine Zahl — und dass eine Schrittweite ≤ 0 (das geleerte
/// Eingabefeld) benannt ungültig ist statt einer Division durch null oder eines
/// endlosen Rasters.</para>
/// </summary>
public sealed class FlottenKandidatenzaehlungTests
{
    // =====================================================================
    //  1. Die Zahlen des Bildschirmfotos
    // =====================================================================

    /// <summary>
    /// Kapazität 50…5000 in Schritten von 10 kWh und Leistung 50…5000 in Schritten von
    /// 20 kW ergeben 496 × 249 = 123 504 Grobpunkte.
    /// </summary>
    /// <remarks>
    /// <b>496 und 249, nicht 496 und 248.</b> Die Kapazität teilt die Spanne 4950 kWh
    /// glatt (495 Schritte + der Anfangswert); die Leistung tut es nicht — 4950 / 20 =
    /// 247,5. Das Raster legt 248 Punkte im Schrittmaß und nimmt die OBERGRENZE dazu,
    /// also 249. Genau so rechnet auch der Lauf; eine zweite Formel in der Oberfläche
    /// liefe hier um einen Punkt auseinander.
    /// </remarks>
    [Fact]
    public void Die_Zahlen_der_Abnahme_stimmen_Punkt_fuer_Punkt()
    {
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(Suchraum(schritt: 10));

        Assert.True(zahl.Gueltig);
        Assert.Equal(FlottenSuchbefund.Inordnung, zahl.Befund);
        Assert.Equal(496L * 249L, zahl.Grob);
        Assert.Equal(123_504L, zahl.Grob);
        Assert.Equal(19L, zahl.FeinHoechstens);
        Assert.Equal(123_523L, zahl.Gesamt);
        Assert.Equal(10_000, zahl.Grenze);
        Assert.False(zahl.Zulaessig);
    }

    /// <summary>
    /// Das Feinraster der Abnahme: zwei Schrittweiten breit, in Neunteln gerastert —
    /// 2 × 9 Teilstücke, die Obergrenze dazu, macht 19 Stützstellen.
    /// </summary>
    [Fact]
    public void Das_Feinraster_der_Abnahme_hat_neunzehn_Stuetzstellen()
    {
        FlottenStudieKonfiguration raum = Suchraum(schritt: 10);

        Assert.Equal(19L, FlottenOptimierer.Kandidatenzahl(raum).FeinHoechstens);

        // Mitten im Suchraum wird das Fenster beidseitig ausgeschoepft — dieselben 19.
        Assert.Equal(19, FlottenOptimierer.Feinrasterwerte(raum.Auslegung.Achsen[0], 2500).Count);

        // Ohne zweite Phase zaehlt nur das Grobraster (die Gegenprobe).
        raum.Auslegung.Feinraster = false;
        FlottenKandidatenzahl ohne = FlottenOptimierer.Kandidatenzahl(raum);
        Assert.Equal(0L, ohne.FeinHoechstens);
        Assert.Equal(123_504L, ohne.Gesamt);
    }

    // =====================================================================
    //  2. Gezählt wird arithmetisch — kein Raster entsteht
    // =====================================================================

    /// <summary>
    /// <b>Eine Schrittweite von 1 kWh ergibt 4951 × 249 = 1 232 799 Kandidaten — und die
    /// Zählung dauert Mikrosekunden.</b> Sie multipliziert Stützstellen, sie legt sie
    /// nicht an.
    /// </summary>
    [Fact]
    public void Die_Kandidatenzahl_entsteht_ohne_Rasteraufbau_und_bleibt_unter_fuenf_Millisekunden()
    {
        FlottenStudieKonfiguration raum = Suchraum(schritt: 1);

        FlottenOptimierer.Kandidatenzahl(raum);          // einmal warmlaufen (JIT)

        const int laeufe = 50;
        var uhr = Stopwatch.StartNew();
        FlottenKandidatenzahl zahl = default;
        for (int i = 0; i < laeufe; i++) zahl = FlottenOptimierer.Kandidatenzahl(raum);
        uhr.Stop();

        Assert.True(zahl.Gueltig);
        Assert.Equal(4951L * 249L, zahl.Grob);
        Assert.Equal(1_232_799L, zahl.Grob);

        double jeLauf = uhr.Elapsed.TotalMilliseconds / laeufe;
        Assert.True(jeLauf < 5.0,
            $"Die Zaehlung braucht {jeLauf:F3} ms je Aufruf — sie baut offenbar ein Raster auf.");
    }

    /// <summary>
    /// <b>Die Gegenprobe zum Rasteraufbau.</b> Eine Schrittweite von einem Millionstel
    /// kWh ergäbe fast fünf Milliarden Stützstellen allein auf der ersten Achse — kein
    /// Rechner legt sie an. Die Zählregel nennt die Zahl trotzdem sofort und weist das
    /// Raster ab.
    /// </summary>
    [Fact]
    public void Ein_Raster_das_sich_nie_bauen_liesse_wird_trotzdem_sofort_gezaehlt()
    {
        var uhr = Stopwatch.StartNew();
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(Suchraum(schritt: 1e-6));
        uhr.Stop();

        Assert.True(zahl.Gueltig);
        Assert.True(zahl.Grob > 1_000_000_000L,
            $"Erwartet wurden Milliarden Stuetzstellen, gezaehlt wurden {zahl.Grob}.");
        Assert.False(zahl.Zulaessig);
        Assert.True(uhr.Elapsed.TotalMilliseconds < 50.0,
            $"Die Zaehlung brauchte {uhr.Elapsed.TotalMilliseconds:F3} ms.");
    }

    // =====================================================================
    //  3. Schrittweite ≤ 0 — das geleerte Eingabefeld
    // =====================================================================

    /// <summary>
    /// <b>Eine Schrittweite von 0 ist benannt ungültig</b> — keine Division durch null,
    /// kein endloses Raster, keine erfundene Zahl. Genau diesen Stand hinterlässt ein
    /// geleertes Eingabefeld des Suchraums; die Oberfläche zeigt daraufhin „Raster
    /// ungültig" und sperrt den Lauf.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(double.NaN)]
    public void Eine_Schrittweite_ohne_Wert_ist_benannt_ungueltig(double schritt)
    {
        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(Suchraum(schritt));

        Assert.False(zahl.Gueltig);
        Assert.False(zahl.Zulaessig);
        Assert.Equal(0L, zahl.Grob);
        Assert.Equal(0L, zahl.FeinHoechstens);
        Assert.Equal(FlottenSuchbefund.GroessenbereichUnbrauchbar, zahl.Befund);
    }

    /// <summary>Dasselbe für eine geleerte UNTERGRENZE: 0 kWh ist kein Suchraum.</summary>
    [Fact]
    public void Eine_Untergrenze_ohne_Wert_ist_ebenfalls_ungueltig()
    {
        FlottenStudieKonfiguration raum = Suchraum(schritt: 10);
        raum.Auslegung.Achsen[0].KapazitaetVonKWh = 0;

        FlottenKandidatenzahl zahl = FlottenOptimierer.Kandidatenzahl(raum);

        Assert.False(zahl.Gueltig);
        Assert.False(zahl.Zulaessig);
    }

    // ================================================================= Prüfstand

    /// <summary>
    /// Der Suchraum des Bildschirmfotos: eine Einheit 4180 kWh / 125 kW / 1 Stück,
    /// Kopplung „Kapazität und Leistung", Kapazität 50…5000 kWh, Leistung 50…5000 kW in
    /// Schritten von 20 kW, Feinraster an, Grenze 10 000.
    /// </summary>
    private static FlottenStudieKonfiguration Suchraum(double schritt)
    {
        var einheit = new FlottenEinheit
        {
            Id = "s1",
            Name = "Speicher 1",
            KapazitaetKWh = 4180,
            LadeleistungKw = 125,
            EntladeleistungKw = 125,
            Ladewirkungsgrad = 0.95,
            Entladewirkungsgrad = 0.95,
            SocMin = 0.1,
            SocMax = 0.9,
            SocStart = 0.5
        };

        return new FlottenStudieKonfiguration
        {
            Einheiten = new List<FlottenEinheit> { einheit },
            Optionen = new FlottenSimulationOptionen
            {
                Betriebsziel = FlottenBetriebsziel.PeakShaving
            },
            Auslegung = new FlottenAuslegungEingang
            {
                MaximaleKandidaten = 10_000,
                Suchmethode = FlottenSuchmethode.Groesse,
                Feinraster = true,
                Achsen = new List<FlottenAuslegungsAchse>
                {
                    new()
                    {
                        Aktiv = true,
                        Modus = FlottenAuslegungsmodus.KapazitaetUndLeistung,
                        AnzahlVon = 1,
                        AnzahlBis = 1,
                        KapazitaetVonKWh = 50,
                        KapazitaetBisKWh = 5000,
                        KapazitaetSchrittKWh = schritt,
                        LeistungVonKw = 50,
                        LeistungBisKw = 5000,
                        LeistungSchrittKw = 20,
                        CRateVon = 0.5,
                        CRateBis = 2.0,
                        CRateSchritt = 0.5,
                        Vorlage = einheit
                    }
                }
            }
        };
    }
}
