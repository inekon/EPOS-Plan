using System;
using System.Collections.Generic;
using System.Globalization;
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

    /// <summary>
    /// Betrieb und Netzwirkung sind immer gerechnet; <c>false</c> heißt, dass
    /// Kapitalwert und Jahreskonten dieses Laufs mangels Kostensätzen NICHT bewertbar
    /// sind (Befund #185). Der Grund steht dann zusätzlich in <see cref="Hinweis"/>.
    /// </summary>
    public bool KostenBewertbar { get; init; } = true;
}

/// <summary>
/// Das Ergebnis der Vorprüfung einer Projektflotte (Befund #185).
/// </summary>
/// <remarks>
/// <b>Probleme</b> verhindern den Flottenlauf; <b>Hinweise</b> tun das nicht. Fehlende
/// Kostensätze sind deshalb ein HINWEIS: Das Betriebsergebnis eines Projektlaufs hängt
/// nicht an ihnen, nur seine wirtschaftliche Bewertung.
/// </remarks>
public sealed class FlottenProjektPruefung
{
    public IReadOnlyList<string> Probleme { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Hinweise { get; init; } = Array.Empty<string>();

    /// <summary>Der Flottenlauf kann ausgeführt werden.</summary>
    public bool Rechenbar => Probleme.Count == 0;

    /// <summary>Alle Probleme in EINER Meldung, samt dem Ausweg am Ende.</summary>
    public string Meldung => Probleme.Count == 0 ? "" :
        MyResource.Resource.FLOTTE_MSG_PROJEKT_UNVOLLSTAENDIG +
        Environment.NewLine + string.Join(Environment.NewLine, Probleme) +
        Environment.NewLine +
        MyResource.Resource.FLOTTE_MSG_PROJEKT_AUSWEG;
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

        // BEFUND #185: Der uebernommene Stand muss VOLLSTAENDIG sein — Quellen, Kosten-
        // quellen UND die aufgeloesten Saetze. Ein Aufrufer, der hier den rohen
        // Dialogstand statt des vorbereiteten Laufstandes einreicht, hinterliess sonst
        // ein @Projektflotte mit "Investitionsquelle = Dialog" und leeren Saetzen; der
        // naechste Projektlauf brach daran ab. Bereits aufgeloeste, brauchbare Saetze
        // bleiben unveraendert eingefroren.
        snapshot.Auslegung.VerwendeteKosten =
            SpeicherAuslegungCtrl.StandKosten(projektId, snapshot.Auslegung);

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

    /// <summary>
    /// Prüft VOR dem Lauf, ob die aktivierte Projektflotte dieses Projekts vollständig
    /// eingerichtet ist (Befund #185).
    /// </summary>
    /// <remarks>
    /// Sie beantwortet in EINEM Durchgang, was der Lauf sonst an fünf verschiedenen
    /// Stellen nacheinander abgebrochen hat: Stand vorhanden, Flotte aktiviert, Einheiten
    /// da, Projektquellen zulässig, planendes Betriebsziel ohne Fahrplan-Löser. Der
    /// Anwender bekommt damit die VOLLSTÄNDIGE Liste statt des jeweils ersten Problems.
    /// </remarks>
    public static FlottenProjektPruefung Pruefe(int projektId)
        => Pruefe(projektId <= 0 ? null : ProjektflottenEingaben(projektId), projektId, true);

    internal static FlottenProjektPruefung Pruefe(SpeicherOptimierungEingaben eingaben,
        int projektId, bool aktivierungGefordert)
    {
        List<string> probleme = new List<string>();
        List<string> hinweise = new List<string>();
        SpeicherAuslegungKonfiguration a = eingaben?.Auslegung;
        if (a == null)
        {
            probleme.Add(MyResource.Resource.FLOTTE_MSG_STAND_FEHLT);
            return new FlottenProjektPruefung { Probleme = probleme, Hinweise = hinweise };
        }

        if (aktivierungGefordert && !a.FlotteImProjektAktiv)
            probleme.Add(MyResource.Resource.FLOTTE_MSG_NICHT_AKTIV);
        if (a.Flotte?.Einheiten == null || a.Flotte.Einheiten.Count == 0)
            probleme.Add(MyResource.Resource.FLOTTE_MSG_KEINE_EINHEITEN);

        // Die fünf Quellenregeln stehen EINMAL — in PruefeProjektquellen. Sie werden
        // hier gerufen und nicht abgeschrieben; die Meldung ist die Problemzeile.
        try { PruefeProjektquellen(a); }
        catch (Exception ex) { probleme.Add(ex.Message); }

        FlottenBetriebsziel ziel = a.Flotte?.Optionen?.Betriebsziel ?? FlottenBetriebsziel.PvGreedy;
        if (FlottenPlanerLage.IstPlanend(ziel) && !FlottenPlanerLage.Verfuegbar)
            probleme.Add(string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.FLOTTE_PLANER_PROFIL, ziel));

        if (SpeicherAuslegungCtrl.SpezifischeSaetzeGebraucht(a) && projektId > 0)
        {
            SpeicherKostensaetze kosten = null;
            try { kosten = SpeicherAuslegungCtrl.StandKosten(projektId, a); }
            catch (Exception ex) { probleme.Add(ex.Message); }
            if (kosten != null && kosten.NichtBewertbar)
                hinweise.Add(MyResource.Resource.FLOTTE_MSG_KOSTEN_NICHT_BEWERTBAR);
        }

        return new FlottenProjektPruefung { Probleme = probleme, Hinweise = hinweise };
    }

    public static SpeicherFlottenProjektLauf Rechnen(SimulationControl sim, int projektId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sim);
        SpeicherOptimierungEingaben eingaben = ProjektflottenEingaben(projektId);
        FlottenProjektPruefung pruefung = Pruefe(eingaben, projektId, true);
        if (!pruefung.Rechenbar) throw new InvalidOperationException(pruefung.Meldung);
        SpeicherAuslegungKonfiguration auslegung = eingaben.Auslegung;

        // Der PROJEKTLAUF darf an fehlenden Kostensaetzen nicht scheitern (Befund #185):
        // Netzleistung, SoC und Energie lesen keinen einzigen von ihnen.
        StromspeicherOptimierungVorbereitung vorbereitung =
            SpeicherAuslegungCtrl.Vorbereiten(sim, projektId, eingaben, KostenPflicht.Projektlauf)
            ?? throw new InvalidOperationException("Die EPOS-Zeitreihen oder Speicherparameter konnten nicht vorbereitet werden.");
        FlottenEingang input = SpeicherFlottenStudieCtrl.Eingang(vorbereitung);
        FlottenStudieKonfiguration config = SpeicherFlottenStudieCtrl.Konfiguration(vorbereitung.Eingaben);
        SpeicherFlottenProjektLauf lauf = Rechnen(input, config, vorbereitung.Kontext,
            Planer(config), cancellationToken);
        bool bewertbar = vorbereitung.Eingaben.Auslegung.VerwendeteKosten?.NichtBewertbar != true;
        string hinweis = Hinweistext(vorbereitung, config, bewertbar);
        return new SpeicherFlottenProjektLauf
        {
            Eingaben = vorbereitung.Eingaben.Kopie(),
            Konfiguration = lauf.Konfiguration, Studie = lauf.Studie,
            Kompatibilitaetsergebnis = lauf.Kompatibilitaetsergebnis,
            Kontext = lauf.Kontext, NetzleistungKw = lauf.NetzleistungKw,
            Hinweis = hinweis, KostenBewertbar = bewertbar
        };
    }

    /// <summary>Zeitachse, Idealwissen und — seit #185 — die fehlende Kostenbewertung.</summary>
    private static string Hinweistext(StromspeicherOptimierungVorbereitung vorbereitung,
        FlottenStudieKonfiguration config, bool kostenBewertbar)
    {
        List<string> zeilen = new List<string> { vorbereitung.ZeitachsenHinweis ?? "" };
        if (config.Optionen.PrognoseArt == PrognoseArt.Oracle)
            zeilen.Add("Idealwissen: Der Flottenplan verwendet zukünftige Werte der Projektzeitreihe als optimistische Vergleichsgrenze.");
        if (!kostenBewertbar) zeilen.Add(MyResource.Resource.FLOTTE_MSG_KOSTEN_NICHT_BEWERTBAR);
        return string.Join(Environment.NewLine,
            zeilen.Where(x => !string.IsNullOrWhiteSpace(x)));
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
        auslegung.FlottenGroessenOptimieren = false;
        // Der Lauf-Snapshot ist noch kein freigegebener Projektstand. Kosten und
        // Projektquellen werden deshalb für diesen Lauf frisch aufgelöst.
        auslegung.FlotteImProjektAktiv = false;
        FlottenProjektPruefung pruefung = Pruefe(laufEingaben, projektId, false);
        if (!pruefung.Rechenbar) throw new InvalidOperationException(pruefung.Meldung);

        StromspeicherOptimierungVorbereitung vorbereitung =
            SpeicherAuslegungCtrl.Vorbereiten(sim, projektId, laufEingaben, KostenPflicht.Projektlauf)
            ?? throw new InvalidOperationException(
                "Die EPOS-Zeitreihen oder Speicherparameter konnten nicht vorbereitet werden.");
        FlottenEingang input = SpeicherFlottenStudieCtrl.Eingang(vorbereitung);
        FlottenStudieKonfiguration config = SpeicherFlottenStudieCtrl.Konfiguration(vorbereitung.Eingaben);
        SpeicherFlottenProjektLauf lauf = Rechnen(input, config, vorbereitung.Kontext,
            Planer(config), cancellationToken);

        bool bewertbar = vorbereitung.Eingaben.Auslegung.VerwendeteKosten?.NichtBewertbar != true;
        return new SpeicherFlottenProjektLauf
        {
            Eingaben = vorbereitung.Eingaben.Kopie(),
            Konfiguration = lauf.Konfiguration,
            Studie = lauf.Studie,
            Kompatibilitaetsergebnis = lauf.Kompatibilitaetsergebnis,
            Kontext = lauf.Kontext,
            NetzleistungKw = lauf.NetzleistungKw,
            Hinweis = Hinweistext(vorbereitung, config, bewertbar),
            KostenBewertbar = bewertbar
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
