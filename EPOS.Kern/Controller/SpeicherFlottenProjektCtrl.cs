using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>Ergebnis der in den gewöhnlichen Projektlauf eingebetteten Speicherflotte.</summary>
public sealed class SpeicherFlottenProjektLauf
{
    /// <summary>Für diesen Lauf aufgelöster, unabhängiger Eingabesnapshot.</summary>
    public SpeicherOptimierungEingaben Eingaben { get; init; }
    public FlottenStudieKonfiguration Konfiguration { get; init; }
    public FlottenStudienErgebnis Studie { get; init; }
    public SpeicherErgebnis Kompatibilitaetsergebnis { get; init; }
    public StromspeicherLaufKontext Kontext { get; init; }
    public string Hinweis { get; init; } = "";
    /// <summary>Netzbezug positiv, Netzeinspeisung negativ [kW].</summary>
    public double[] NetzleistungKw { get; init; } = Array.Empty<double>();
}

/// <summary>
/// Übernimmt einen freigegebenen Flottenkandidaten in den reservierten Stand
/// <c>@Projektflotte</c> und rechnet ihn im vollständigen EPOS-Projektlauf.
/// Es werden keine Kostenpositionen des Projekts geändert.
/// </summary>
public static class SpeicherFlottenProjektCtrl
{
    /// <summary>Reservierter, vom Studien-Arbeitsstand unabhängiger Projektflottenstand.</summary>
    public const string ProjektflottenStand = "@Projektflotte";

    /// <summary>
    /// Vom Host einzusetzende Planerfabrik. Reaktive Ziele benötigen keinen Planer;
    /// PvPlanung, Arbitrage und MultiUse schlagen ohne Fabrik ausdrücklich fehl.
    /// </summary>
    public static Func<IFlottenPlaner> PlanerFactory { get; set; }

    public static void Aktivieren(int projektId, SpeicherFlottenErgebnis ergebnis)
    {
        if (projektId <= 0) throw new ArgumentOutOfRangeException(nameof(projektId));
        PruefeUebernahme(ergebnis);

        SpeicherOptimierungEingaben snapshot = ergebnis.Eingaben.Kopie();
        snapshot.Auslegung.Flotte = SpeicherAuslegungKopie.Von(ergebnis.Konfiguration);
        snapshot.Auslegung.Flotte.Auslegung.Achsen.Clear();
        snapshot.Auslegung.FlottenGroessenOptimieren = false;
        snapshot.Auslegung.FlotteImProjektAktiv = true;
        snapshot.Auslegung.Revision = checked(snapshot.Auslegung.Revision + 1);

        // Der Projektflottenstand gehoert zum Projekt, nicht zur gerade aktiven
        // Einzelanlage. anlageId 0 wird als DB-NULL gespeichert und uebersteht damit
        // Variantenwechsel und das Loeschen einer einzelnen Anlagenzeile.
        SpeicherAuslegungCtrl.Speichern(projektId, 0,
            ProjektflottenStand, snapshot);
    }

    public static void Deaktivieren(int projektId)
    {
        if (projektId <= 0) throw new ArgumentOutOfRangeException(nameof(projektId));
        SpeicherOptimierungEingaben eingaben = ProjektflottenEingaben(projektId);
        if (eingaben?.Auslegung == null || !eingaben.Auslegung.FlotteImProjektAktiv) return;
        eingaben.Auslegung.FlotteImProjektAktiv = false;
        eingaben.Auslegung.Revision = checked(eingaben.Auslegung.Revision + 1);
        SpeicherAuslegungCtrl.Speichern(projektId, 0,
            ProjektflottenStand, eingaben);
    }

    public static bool IstAktiv(int projektId)
    {
        if (projektId <= 0) return false;
        return ProjektflottenEingaben(projektId)?.Auslegung?.FlotteImProjektAktiv == true;
    }

    /// <summary>Unabhängiger Lesestand der aktivierten Einheiten für die Projektdarstellung.</summary>
    public static FlottenStudieKonfiguration AktiveKonfiguration(int projektId)
    {
        if (projektId <= 0) return null;
        var a = ProjektflottenEingaben(projektId)?.Auslegung;
        return a?.FlotteImProjektAktiv == true ? SpeicherAuslegungKopie.Von(a.Flotte) : null;
    }

    public static SpeicherFlottenProjektLauf Rechnen(SimulationControl sim, int projektId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sim);
        SpeicherOptimierungEingaben eingaben = ProjektflottenEingaben(projektId)
            ?? throw new InvalidOperationException("Der reservierte Projektflottenstand @Projektflotte fehlt.");
        SpeicherAuslegungKonfiguration auslegung = eingaben.Auslegung
            ?? throw new InvalidOperationException("Die aktive Flottenkonfiguration fehlt.");
        if (!auslegung.FlotteImProjektAktiv)
            throw new InvalidOperationException("Die Speicherflotte ist für den Projektlauf nicht aktiviert.");
        PruefeProjektquellen(auslegung);

        StromspeicherOptimierungVorbereitung vorbereitung =
            SpeicherAuslegungCtrl.Vorbereiten(sim, projektId, eingaben)
            ?? throw new InvalidOperationException("Die EPOS-Zeitreihen oder Speicherparameter konnten nicht vorbereitet werden.");
        FlottenEingang input = SpeicherFlottenStudieCtrl.Eingang(vorbereitung);
        FlottenStudieKonfiguration config = SpeicherFlottenStudieCtrl.Konfiguration(vorbereitung.Eingaben);
        SpeicherFlottenProjektLauf lauf = Rechnen(input, config, vorbereitung.Kontext,
            Planer(config), cancellationToken);
        string hinweis = vorbereitung.ZeitachsenHinweis ?? "";
        if (config.Optionen.PrognoseArt == PrognoseArt.Oracle)
            hinweis = string.Join(Environment.NewLine, new[]
            {
                hinweis,
                "Idealwissen: Der Flottenplan verwendet zukünftige Werte der Projektzeitreihe als optimistische Vergleichsgrenze."
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new SpeicherFlottenProjektLauf
        {
            Eingaben = vorbereitung.Eingaben.Kopie(),
            Konfiguration = lauf.Konfiguration, Studie = lauf.Studie,
            Kompatibilitaetsergebnis = lauf.Kompatibilitaetsergebnis,
            Kontext = lauf.Kontext, NetzleistungKw = lauf.NetzleistungKw,
            Hinweis = hinweis
        };
    }

    /// <summary>
    /// Rechnet einen nur an diese Simulationsinstanz gebundenen Flottenstand mit den
    /// gerade erzeugten EPOS-Projektreihen. Der Stand wird weder aktiviert noch in der
    /// Datenbank gespeichert.
    /// </summary>
    public static SpeicherFlottenProjektLauf Rechnen(SimulationControl sim, int projektId,
        SpeicherOptimierungEingaben snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sim);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (projektId <= 0) throw new ArgumentOutOfRangeException(nameof(projektId));

        SpeicherOptimierungEingaben laufEingaben = snapshot.Kopie();
        SpeicherAuslegungKonfiguration auslegung = laufEingaben.Auslegung
            ?? throw new InvalidOperationException("Die Flottenkonfiguration des Lauf-Snapshots fehlt.");
        if (auslegung.Flotte?.Einheiten == null || auslegung.Flotte.Einheiten.Count == 0)
            throw new InvalidOperationException(
                "Der Lauf-Snapshot enthält keine physische Speicherflotte.");
        auslegung.FlottenGroessenOptimieren = false;
        // Der Lauf-Snapshot ist noch kein freigegebener Projektstand. Kosten und
        // Projektquellen werden deshalb für diesen Lauf frisch aufgelöst.
        auslegung.FlotteImProjektAktiv = false;
        PruefeProjektquellen(auslegung);

        StromspeicherOptimierungVorbereitung vorbereitung =
            SpeicherAuslegungCtrl.Vorbereiten(sim, projektId, laufEingaben)
            ?? throw new InvalidOperationException(
                "Die EPOS-Zeitreihen oder Speicherparameter konnten nicht vorbereitet werden.");
        FlottenEingang input = SpeicherFlottenStudieCtrl.Eingang(vorbereitung);
        FlottenStudieKonfiguration config = SpeicherFlottenStudieCtrl.Konfiguration(vorbereitung.Eingaben);
        SpeicherFlottenProjektLauf lauf = Rechnen(input, config, vorbereitung.Kontext,
            Planer(config), cancellationToken);

        string hinweis = vorbereitung.ZeitachsenHinweis ?? "";
        if (config.Optionen.PrognoseArt == PrognoseArt.Oracle)
            hinweis = string.Join(Environment.NewLine, new[]
            {
                hinweis,
                "Idealwissen: Der Flottenplan verwendet zukünftige Werte der Projektzeitreihe als optimistische Vergleichsgrenze."
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

        return new SpeicherFlottenProjektLauf
        {
            Eingaben = vorbereitung.Eingaben.Kopie(),
            Konfiguration = lauf.Konfiguration,
            Studie = lauf.Studie,
            Kompatibilitaetsergebnis = lauf.Kompatibilitaetsergebnis,
            Kontext = lauf.Kontext,
            NetzleistungKw = lauf.NetzleistungKw,
            Hinweis = hinweis
        };
    }

    internal static SpeicherFlottenProjektLauf Rechnen(FlottenEingang input,
        FlottenStudieKonfiguration config, StromspeicherLaufKontext kontext,
        IFlottenPlaner planer = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(config);
        FlottenStudieKonfiguration snapshot = SpeicherAuslegungKopie.Von(config);
        FlottenStudienErgebnis studie = FlottenSimulator.Simuliere(input, snapshot, planer, cancellationToken);
        if (!studie.Variante.Zulaessig)
            throw new InvalidOperationException("Die aktivierte Speicherflotte ist unzulässig: " +
                (studie.Variante.Unzulaessigkeitsgrund ?? "kein Grund angegeben"));
        if (studie.Variante.Intervalle.Count != input.Istwerte.Count)
            throw new InvalidOperationException("Flottenergebnis und Projektzeitreihe haben verschiedene Längen.");

        StromspeicherLaufKontext laufkontext = kontext ?? new StromspeicherLaufKontext();
        laufkontext.Flottenkonfiguration = SpeicherAuslegungKopie.Von(snapshot);
        laufkontext.Flottenergebnis = studie;
        if (laufkontext.Parameter != null)
        {
            double kapazitaet = snapshot.Einheiten.Sum(x => x.KapazitaetKWh);
            laufkontext.Parameter = laufkontext.Parameter with
            {
                CNomKwh = kapazitaet,
                PKw = snapshot.Einheiten.Sum(x => Math.Max(x.LadeleistungKw, x.EntladeleistungKw)),
                SoCMinKwh = snapshot.Einheiten.Sum(x => x.KapazitaetKWh * x.SocMin),
                SoCMaxKwh = snapshot.Einheiten.Sum(x => x.KapazitaetKWh * x.SocMax),
                StartSoCKwh = snapshot.Einheiten.Sum(x => x.KapazitaetKWh * x.SocStart)
            };
        }

        return new SpeicherFlottenProjektLauf
        {
            Konfiguration = SpeicherAuslegungKopie.Von(snapshot),
            Studie = studie,
            Kompatibilitaetsergebnis = StromspeicherSimCtrl.AlsKompatibilitaetsergebnis(studie, input, snapshot),
            Kontext = laufkontext,
            NetzleistungKw = StromspeicherSimCtrl.NetzleistungKw(studie)
        };
    }

    internal static void PruefeUebernahme(SpeicherFlottenErgebnis ergebnis)
    {
        ArgumentNullException.ThrowIfNull(ergebnis);
        if (!ergebnis.Erfolg || ergebnis.Abgebrochen)
            throw new InvalidOperationException("Nur ein erfolgreich abgeschlossener Flottenlauf kann übernommen werden.");
        if (ergebnis.Eingaben?.Auslegung == null || ergebnis.Konfiguration == null || ergebnis.Studie == null)
            throw new InvalidOperationException("Der Flottenlauf enthält keinen vollständigen Eingabesnapshot.");
        if (ergebnis.Konfiguration.Einheiten == null || ergebnis.Konfiguration.Einheiten.Count == 0)
            throw new InvalidOperationException("Die Nullvariante kann nicht als Projektflotte übernommen werden.");
        if (ergebnis.Auslegung?.NullvarianteGewonnen == true)
            throw new InvalidOperationException("Die Nullvariante hat die Auslegung gewonnen und kann nicht aktiviert werden.");
        if (ergebnis.Auslegung is { } auslegung)
        {
            if (auslegung.BesterKandidat == null || !auslegung.BesterKandidat.Zulaessig)
                throw new InvalidOperationException("Die Auslegung enthält keinen zulässigen besten Kandidaten.");
            string kandidatId = auslegung.BesterKandidat.KandidatId;
            if (string.IsNullOrWhiteSpace(kandidatId) ||
                kandidatId != ergebnis.Studie.Variante.KonfigurationId ||
                kandidatId != auslegung.BesteZeitreihe?.KonfigurationId ||
                kandidatId != auslegung.BesteStudie?.Variante.KonfigurationId)
                throw new InvalidOperationException("Kandidat, beste Zeitreihe und Studie gehören nicht zum selben Auslegungsstand.");
            if (auslegung.BesteKonfiguration == null ||
                JsonSerializer.Serialize(auslegung.BesteKonfiguration) != JsonSerializer.Serialize(ergebnis.Konfiguration))
                throw new InvalidOperationException("Die übernommene Konfiguration gehört nicht zum besten Auslegungskandidaten.");
        }
        if (!ergebnis.Studie.Variante.Zulaessig)
            throw new InvalidOperationException("Die gewählte Flotte ist unzulässig: " +
                (ergebnis.Studie.Variante.Unzulaessigkeitsgrund ?? "kein Grund angegeben"));
        if (ergebnis.Studie.Variante.Intervalle.Count == 0 ||
            ergebnis.Studie.Variante.Intervalle.Count != ergebnis.Studie.ReferenzOhneSpeicher.Intervalle.Count)
            throw new InvalidOperationException("Der Flottenlauf enthält keine vollständige Referenz- und Variantenzeitreihe.");
        if (string.IsNullOrWhiteSpace(ergebnis.Studie.Variante.DatenId) ||
            ergebnis.Studie.Variante.DatenId != ergebnis.Studie.ReferenzOhneSpeicher.DatenId)
            throw new InvalidOperationException("Referenz und Variante stammen nicht aus demselben Datenstand.");

        string erwarteteKonfiguration = ergebnis.Auslegung == null
            ? Hash(ergebnis.Eingaben.Auslegung)
            : ergebnis.Auslegung.BesterKandidat.KandidatId;
        if (!string.Equals(erwarteteKonfiguration, ergebnis.Studie.Variante.KonfigurationId,
                StringComparison.Ordinal) || ergebnis.Studie.Variante.KonfigurationId !=
            ergebnis.Studie.ReferenzOhneSpeicher.KonfigurationId)
            throw new InvalidOperationException("Das Ergebnis ist nicht mehr aktuell für den enthaltenen Konfigurationsstand.");
        PruefeProjektquellen(ergebnis.Eingaben.Auslegung);
    }

    internal static void PruefeProjektquellen(SpeicherAuslegungKonfiguration a)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (a.Lastquelle != SpeicherAuslegungQuelle.Epos)
            throw new InvalidOperationException("Eine Projektflotte muss die Last aus der laufenden EPOS-Projektsimulation verwenden; eine externe Lastdatei kann den gesamten Projektstrombedarf nicht still ersetzen.");
        if (a.PvQuelle != SpeicherAuslegungQuelle.Epos && a.PvQuelle != SpeicherAuslegungQuelle.Keine)
            throw new InvalidOperationException("Eine Projektflotte muss die PV-Erzeugung aus der laufenden EPOS-Projektsimulation verwenden; eine externe PV-Datei kann die Projektanlage nicht still ersetzen.");
        if (a.FlottenProjektjahre?.Count > 0)
            throw new InvalidOperationException("Externe Flotten-Projektjahre gehören zur eigenständigen Studie und dürfen den gewöhnlichen EPOS-Projektlauf nicht ersetzen.");
        if (a.Preisquelle == SpeicherAuslegungQuelle.Datei)
        {
            if (!a.EposModelljahrZuordnen)
                throw new InvalidOperationException("Eine Preisdatei im Projektlauf benötigt die ausdrücklich gewählte EPOS-Modelljahrzuordnung.");
            if (a.PreisDatei?.Werte == null || a.PreisDatei.ZeitstempelUtc == null ||
                a.PreisDatei.Werte.Length != 35040 || a.PreisDatei.ZeitstempelUtc.Length != 35040)
                throw new InvalidOperationException("Die Preisdatei muss für den Projektlauf genau 35.040 ausgerichtete Viertelstunden enthalten.");
        }
    }

    private static IFlottenPlaner Planer(FlottenStudieKonfiguration config)
    {
        bool erforderlich = config.Optionen.Betriebsziel is FlottenBetriebsziel.PvPlanung
            or FlottenBetriebsziel.Arbitrage or FlottenBetriebsziel.MultiUse;
        if (!erforderlich) return null;
        if (PlanerFactory == null)
            throw new InvalidOperationException("Für das gewählte Flottenziel wurde keine IFlottenPlaner-Fabrik registriert.");
        return PlanerFactory() ?? throw new InvalidOperationException("Die IFlottenPlaner-Fabrik hat keinen Planer geliefert.");
    }

    private static SpeicherOptimierungEingaben ProjektflottenEingaben(int projektId)
    {
        return SpeicherAuslegungCtrl.Profile(projektId, 0)
            .FirstOrDefault(x => x.Name == ProjektflottenStand)?.Eingaben?.Kopie();
    }

    // Muss exakt dieselbe kanonische Darstellung wie SpeicherFlottenStudieCtrl.Eingang verwenden.
    private static string Hash<T>(T wert) => Convert.ToHexString(
        SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(wert)));
}
