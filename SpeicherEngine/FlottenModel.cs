using System;
using System.Collections.Generic;
using System.Threading;

namespace SpeicherEngine;

public enum FlottenBetriebsziel
{
    PvGreedy,
    PeakShaving,
    PvPlanung,
    Arbitrage,
    MultiUse
}

public enum FlottenVerteilung
{
    KapazitaetsProportional,
    Kaskade,
    Grenzkosten
}

public enum FlottenAuslegungsmodus
{
    KapazitaetUndLeistung,
    KapazitaetUndCRate,
    LeistungUndCRate
}

public enum FlottenErzeugerPrioritaet
{
    PvVorBhkw,
    BhkwVorPv
}

public enum PrognoseArt
{
    VerifiziertBekannt,
    Oracle
}

public enum FlottenPlanStatus
{
    Optimal,
    Zulaessig,
    Unzulaessig,
    Zeitlimit,
    Fehler
}

public enum FlottenPlanerModus
{
    PvPlanung,
    Arbitrage,
    MultiUse
}

public enum FlottenEndbedingung
{
    KeineVorgabe,
    JeSpeicherZiel,
    JeSpeicherWieAnfang
}

public sealed class FlottenEinheit
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AnlageId { get; set; }
    public bool EigeneKosten { get; set; }
    public double KapazitaetKWh { get; set; }
    public double LadeleistungKw { get; set; }
    public double EntladeleistungKw { get; set; }
    public double Ladewirkungsgrad { get; set; } = 0.95;
    public double Entladewirkungsgrad { get; set; } = 0.95;
    public double SocMin { get; set; } = 0.10;
    public double SocMax { get; set; } = 0.90;
    public double SocStart { get; set; } = 0.50;
    public double PeakReserveKWh { get; set; }
    public double HilfsverbrauchKw { get; set; }
    public double GrenzverschleissEuroProKWhEntladung { get; set; }
    public double InvestitionEuro { get; set; }
    public double InvestitionEuroProKWh { get; set; }
    public double InvestitionEuroProKw { get; set; }
    public double JaehrlicheFixeOpexEuro { get; set; }
    public double JaehrlicheOpexEuroProKWhKapazitaet { get; set; }
    public double JaehrlicheOpexEuroProKw { get; set; }
    public double DurchsatzkostenEuroProKWhEntladung { get; set; }
    public double ErsatzkostenEuro { get; set; }
    public int ErsatzintervallJahre { get; set; }
    public double RestwertEuro { get; set; }
    public List<FlottenRainflowPunkt> RainflowKurve { get; set; } = new();
}

public sealed class FlottenRainflowPunkt
{
    public double Entladetiefe { get; set; }
    public double ZyklenBisEol { get; set; }
}

public sealed class FlottenNetzintervall
{
    public DateTimeOffset Zeitstempel { get; set; }
    public double LastKw { get; set; }
    public double PvKw { get; set; }
    public double BhkwKw { get; set; }
    public double BezugspreisEuroProKWh { get; set; }
    public double PvVerkaufspreisEuroProKWh { get; set; }
    public double BhkwVerkaufspreisEuroProKWh { get; set; }
    public double BatterieVerkaufspreisEuroProKWh { get; set; }
}

/// <summary>Unveraenderlicher Informationsstand, der einem Planer nachweislich bekannt war.</summary>
public sealed class FlottenPrognoseSnapshot
{
    private readonly FlottenNetzintervall[] _intervalle;

    public FlottenPrognoseSnapshot(
        string id,
        DateTimeOffset bekanntSeit,
        DateTimeOffset entscheidungszeitpunkt,
        PrognoseArt art,
        IReadOnlyList<FlottenNetzintervall> intervalle)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        BekanntSeit = bekanntSeit;
        Entscheidungszeitpunkt = entscheidungszeitpunkt;
        Art = art;
        _intervalle = new FlottenNetzintervall[intervalle?.Count ?? throw new ArgumentNullException(nameof(intervalle))];
        for (var i = 0; i < _intervalle.Length; i++)
        {
            var x = intervalle[i];
            _intervalle[i] = new FlottenNetzintervall
            {
                Zeitstempel = x.Zeitstempel,
                LastKw = x.LastKw,
                PvKw = x.PvKw,
                BhkwKw = x.BhkwKw,
                BezugspreisEuroProKWh = x.BezugspreisEuroProKWh,
                PvVerkaufspreisEuroProKWh = x.PvVerkaufspreisEuroProKWh,
                BhkwVerkaufspreisEuroProKWh = x.BhkwVerkaufspreisEuroProKWh,
                BatterieVerkaufspreisEuroProKWh = x.BatterieVerkaufspreisEuroProKWh
            };
        }

        Intervalle = Array.AsReadOnly(_intervalle);
    }

    private FlottenPrognoseSnapshot(string id, DateTimeOffset bekanntSeit,
        DateTimeOffset entscheidungszeitpunkt, PrognoseArt art,
        FlottenNetzintervall[] owned, int start, int count)
    {
        Id = id;
        BekanntSeit = bekanntSeit;
        Entscheidungszeitpunkt = entscheidungszeitpunkt;
        Art = art;
        _intervalle = owned;
        Intervalle = new ArraySegment<FlottenNetzintervall>(owned, start, count);
    }

    public string Id { get; }
    public DateTimeOffset BekanntSeit { get; }
    public DateTimeOffset Entscheidungszeitpunkt { get; }
    public PrognoseArt Art { get; }
    public IReadOnlyList<FlottenNetzintervall> Intervalle { get; }

    public FlottenPrognoseSnapshot Ausschnitt(DateTimeOffset entscheidungszeitpunkt, int maximalIntervalle)
    {
        if (maximalIntervalle <= 0) throw new ArgumentOutOfRangeException(nameof(maximalIntervalle));
        var start = Array.FindIndex(_intervalle, x => x.Zeitstempel == entscheidungszeitpunkt);
        if (start < 0) throw new ArgumentException("Snapshot deckt den Entscheidungszeitpunkt nicht ab.");
        return new FlottenPrognoseSnapshot(Id, BekanntSeit, entscheidungszeitpunkt, Art,
            _intervalle, start, Math.Min(maximalIntervalle, _intervalle.Length - start));
    }
}

public sealed class FlottenPlanAnfrage
{
    public DateTimeOffset Entscheidungszeitpunkt { get; set; }
    public FlottenPlanerModus Modus { get; set; }
    public FlottenPrognoseSnapshot Prognose { get; set; } = null!;
    public List<FlottenEinheit> Einheiten { get; set; } = new();
    public List<double> EnergieKWh { get; set; } = new();
    /// <summary>Je Speicher und Prognoseintervall ein Faktor von 0 bis 1.</summary>
    public List<List<double>> VerfuegbarkeitsfaktorJeSpeicher { get; set; } = new();
    public FlottenSimulationOptionen Optionen { get; set; } = new();
    public double BisherigerAbrechnungspeakKw { get; set; }
    public double LeistungspreisEuroProKw { get; set; }
    public double EndenergieAusgleichEuroProKWh { get; set; }
    public TimeSpan Zeitlimit { get; set; } = TimeSpan.FromSeconds(60);
}

public sealed class FlottenPlan
{
    public string PlanId { get; set; } = string.Empty;
    public string PrognoseId { get; set; } = string.Empty;
    public DateTimeOffset Entscheidungszeitpunkt { get; set; }
    public FlottenPlanStatus Status { get; set; }
    public string? StatusText { get; set; }
    public double ZielfunktionswertEuro { get; set; }
    public double GeplanteImportverletzungKw { get; set; }
    public List<FlottenPlanIntervall> Intervalle { get; set; } = new();
}

public sealed class FlottenPlanIntervall
{
    public DateTimeOffset Zeitstempel { get; set; }
    public List<double> LadeleistungKwJeSpeicher { get; set; } = new();
    public List<double> EntladeleistungKwJeSpeicher { get; set; } = new();
    public double PvAbregelungKw { get; set; }
}

public interface IFlottenPlaner
{
    FlottenPlan Plane(FlottenPlanAnfrage anfrage, CancellationToken cancellationToken);
}

public sealed class FlottenSimulationOptionen
{
    public FlottenBetriebsziel Betriebsziel { get; set; } = FlottenBetriebsziel.PvGreedy;
    public FlottenVerteilung Verteilung { get; set; } = FlottenVerteilung.KapazitaetsProportional;
    public double? NetzbezugGrenzeKw { get; set; }
    public double? NetzeinspeisungGrenzeKw { get; set; }
    public bool NetzladungErlaubt { get; set; }
    public bool BatterieexportErlaubt { get; set; }
    public double? WirtschaftlicherPeakZielwertKw { get; set; }
    public double? ArbitrageLadepreisSchwelle { get; set; }
    public double? ArbitrageEntladepreisSchwelle { get; set; }
    public PrognoseArt PrognoseArt { get; set; } = PrognoseArt.VerifiziertBekannt;
    public FlottenErzeugerPrioritaet ErzeugerPrioritaet { get; set; } = FlottenErzeugerPrioritaet.PvVorBhkw;
    public int PlanungshorizontIntervalle { get; set; } = 192;
    public int NeuplanungAlleIntervalle { get; set; } = 1;
    public FlottenEndbedingung Endbedingung { get; set; } = FlottenEndbedingung.KeineVorgabe;
    public List<double> EndenergieZielKWh { get; set; } = new();
    public double? EnergieAusgleichEuroProKWh { get; set; }
    public bool PrognoseFallbackErlaubt { get; set; }
}

public sealed class FlottenEingang
{
    public string KonfigurationId { get; set; } = string.Empty;
    public string DatenId { get; set; } = string.Empty;
    public List<FlottenNetzintervall> Istwerte { get; set; } = new();
    public List<FlottenPrognoseSnapshot> Prognosen { get; set; } = new();
    public List<FlottenProjektjahr> Projektjahre { get; set; } = new();
    public Dictionary<string, List<double>> VerfuegbarkeitsfaktorNachEinheitId { get; set; } = new();
}

public sealed class FlottenProjektjahr
{
    public int Jahr { get; set; }
    public List<FlottenNetzintervall> Istwerte { get; set; } = new();
    public List<FlottenPrognoseSnapshot> Prognosen { get; set; } = new();
    public Dictionary<string, List<double>> VerfuegbarkeitsfaktorNachEinheitId { get; set; } = new();
    public bool IstVollstaendigesJahr { get; set; } = true;
}

public sealed class FlottenStudieKonfiguration
{
    public List<FlottenEinheit> Einheiten { get; set; } = new();
    public FlottenSimulationOptionen Optionen { get; set; } = new();
    public FlottenTarif Tarif { get; set; } = new();
    public FlottenWirtschaftlichkeitEingang Wirtschaftlichkeit { get; set; } = new();
    public FlottenAuslegungEingang Auslegung { get; set; } = new();
}

public sealed class FlottenIntervallErgebnis
{
    public DateTimeOffset Zeitstempel { get; set; }
    public double LastKw { get; set; }
    public double PvVerfuegbarKw { get; set; }
    public double BhkwKw { get; set; }
    public double HilfsverbrauchKw { get; set; }
    public double NetzleistungKw { get; set; }
    public double NetzbezugKw { get; set; }
    public double NetzeinspeisungKw { get; set; }
    public double PvNetzeinspeisungKw { get; set; }
    public double BhkwNetzeinspeisungKw { get; set; }
    public double BatterieNetzeinspeisungKw { get; set; }
    public double PvAbregelungKw { get; set; }
    public double UmwandlungsverlustKWh { get; set; }
    public List<double> UmwandlungsverlustKWhJeSpeicher { get; set; } = new();
    public double SollabweichungKw { get; set; }
    public double TechnischeImportverletzungKw { get; set; }
    public double TechnischeExportverletzungKw { get; set; }
    public double WirtschaftlichePeakverletzungKw { get; set; }
    public List<double> SollleistungKwJeSpeicher { get; set; } = new();
    public List<double> IstleistungKwJeSpeicher { get; set; } = new();
    public List<double> EnergieStartKWhJeSpeicher { get; set; } = new();
    public List<double> EnergieEndeKWhJeSpeicher { get; set; } = new();
    public string? PlanId { get; set; }
    public string? PrognoseId { get; set; }
    public bool PlanFallback { get; set; }
    public string? PlanFallbackGrund { get; set; }
}

public sealed class FlottenSpeicherKennzahlen
{
    public string SpeicherId { get; set; } = string.Empty;
    public double AnfangsenergieKWh { get; set; }
    public double EndenergieKWh { get; set; }
    public double LadeenergieAcKWh { get; set; }
    public double EntladeenergieAcKWh { get; set; }
    public double AequivalenteVollzyklen { get; set; }
    public double RainflowSchaden { get; set; }
    public List<FlottenRainflowZyklus> RainflowZyklen { get; set; } = new();
}

public sealed class FlottenRainflowZyklus
{
    public double Entladetiefe { get; set; }
    public double MittlererSoc { get; set; }
    public double Anzahl { get; set; }
}

public sealed class FlottenRainflowErgebnis
{
    public List<FlottenRainflowZyklus> Zyklen { get; set; } = new();
    public double Schaden { get; set; }
}

public sealed class FlottenSimulationErgebnis
{
    public string KonfigurationId { get; set; } = string.Empty;
    public string DatenId { get; set; } = string.Empty;
    public bool Zulaessig { get; set; }
    public string? Unzulaessigkeitsgrund { get; set; }
    public List<FlottenIntervallErgebnis> Intervalle { get; set; } = new();
    public List<FlottenSpeicherKennzahlen> SpeicherKennzahlen { get; set; } = new();
    public double NetzbezugKWh { get; set; }
    public double NetzeinspeisungKWh { get; set; }
    public double PvAbregelungKWh { get; set; }
    public double VerlusteKWh { get; set; }
    public double MaximalerNetzbezugKw { get; set; }
    public int PlanFallbackIntervalle { get; set; }
}

public sealed class FlottenStudienErgebnis
{
    public FlottenSimulationErgebnis ReferenzOhneSpeicher { get; set; } = new();
    public FlottenSimulationErgebnis Variante { get; set; } = new();
    public FlottenRechnung Referenzrechnung { get; set; } = new();
    public FlottenRechnung Variantenrechnung { get; set; } = new();
    public List<double> EndenergieAenderungKWhJeSpeicher { get; set; } = new();
    public double EndenergieAusgleichEuro { get; set; }
    public FlottenWirtschaftlichkeitErgebnis? Wirtschaftlichkeit { get; set; }
}

public sealed class FlottenTarif
{
    public double LeistungspreisEuroProKw { get; set; }
    public double FixkostenEuro { get; set; }
    public double? BatterieVerkaufspreisEuroProKWh { get; set; }
}

public sealed class FlottenRechnung
{
    public double EnergiekostenEuro { get; set; }
    public double LeistungskostenEuro { get; set; }
    public double FixkostenEuro { get; set; }
    public double PeakKw { get; set; }
    public double GesamtEuro { get; set; }
}

public sealed class FlottenJahreskonto
{
    public int Jahr { get; set; }
    public FlottenRechnung Referenzrechnung { get; set; } = new();
    public FlottenRechnung Variantenrechnung { get; set; } = new();
    public double OpexEuro { get; set; }
    public double DurchsatzkostenEuro { get; set; }
    public double ErsatzkostenEuro { get; set; }
    public double EndenergieAusgleichEuro { get; set; }
    public double NettoCashflowEuro { get; set; }
    public bool IstVollstaendigesJahr { get; set; } = true;
    public string Projektionskennzeichnung { get; set; } = string.Empty;
}

public sealed class FlottenWirtschaftlichkeitEingang
{
    public List<FlottenEinheit> Einheiten { get; set; } = new();
    public List<FlottenJahreskonto> Jahreskonten { get; set; } = new();
    public double Kalkulationszins { get; set; }
    public double RestwertEuro { get; set; }
    public bool ReferenzjahrExplizitWiederholen { get; set; }
    public int ProjektjahreBeiWiederholung { get; set; }
}

public sealed class FlottenWirtschaftlichkeitErgebnis
{
    public double InvestitionEuro { get; set; }
    public double KapitalwertEuro { get; set; }
    public int? DiskontierteAmortisationJahr { get; set; }
    public List<double> JahresCashflowsEuro { get; set; } = new();
    public List<FlottenJahreskonto> Jahreskonten { get; set; } = new();
    public bool IstWiederholteReferenzjahrProjektion { get; set; }
}

public sealed class FlottenAuslegungsAchse
{
    public bool Aktiv { get; set; } = true;
    public string? ErsetztEinheitId { get; set; }
    public FlottenAuslegungsmodus Modus { get; set; }
    public int AnzahlVon { get; set; } = 1;
    public int AnzahlBis { get; set; } = 1;
    public double KapazitaetVonKWh { get; set; }
    public double KapazitaetBisKWh { get; set; }
    public double KapazitaetSchrittKWh { get; set; }
    public double LeistungVonKw { get; set; }
    public double LeistungBisKw { get; set; }
    public double LeistungSchrittKw { get; set; }
    public double CRateVon { get; set; }
    public double CRateBis { get; set; }
    public double CRateSchritt { get; set; }
    public FlottenEinheit Vorlage { get; set; } = new();
}

public sealed class FlottenAuslegungEingang
{
    public List<FlottenAuslegungsAchse> Achsen { get; set; } = new();
    public List<FlottenBetriebsziel> Betriebsziele { get; set; } = new();
    public int MaximaleKandidaten { get; set; } = 10000;
}

public sealed class FlottenKandidatZusammenfassung
{
    public string KandidatId { get; set; } = string.Empty;
    public FlottenBetriebsziel Betriebsziel { get; set; }
    public bool Zulaessig { get; set; }
    public double KapitalwertEuro { get; set; }
    public double KapazitaetKWh { get; set; }
    public double LadeleistungKw { get; set; }
    public double EntladeleistungKw { get; set; }
    public string? Grund { get; set; }
}

public sealed class FlottenAuslegungErgebnis
{
    public bool NullvarianteGewonnen { get; set; }
    public bool NullvarianteZulaessig { get; set; }
    public string Aussage { get; set; } = "Beste Variante im geprueften endlichen Raster";
    public FlottenKandidatZusammenfassung? BesterKandidat { get; set; }
    public FlottenSimulationErgebnis? BesteZeitreihe { get; set; }
    public FlottenStudieKonfiguration? BesteKonfiguration { get; set; }
    public FlottenStudienErgebnis? BesteStudie { get; set; }
    public List<FlottenKandidatZusammenfassung> Kandidaten { get; set; } = new();
}

public sealed class FlottenFortschritt
{
    public int Abgeschlossen { get; set; }
    public int Gesamt { get; set; }
    public string KandidatId { get; set; } = string.Empty;
}
