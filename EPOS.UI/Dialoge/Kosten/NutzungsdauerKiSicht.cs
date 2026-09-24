using KiKern;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Das FLACHE Abbild der Maske „Nutzungsdauern (AfA)" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Kopffelder und SPALTEN.</b> Der Kopf trägt den Filter und die sechs
/// Felder der Neuzeile; die Tabelle darunter wird an Ort und Stelle bearbeitet und
/// kommt deshalb als Spalten in den Katalog — dieselbe Bauart wie das
/// Positionsraster der Kostenverwaltung. Aus jeder Spaltendeklaration wird je
/// vorhandener Zeile ein gewöhnliches Feld
/// (<c>nutzungsdauer_1</c>, <c>nutzungsdauer_2</c>, …), und den Klartextnamen einer
/// Zeile liefert ihre Positionsart.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff
/// ihren Delegaten. <see cref="Zeilen"/> liefert die LEBENDE Liste des Dialogs —
/// Zeilen entstehen und vergehen, solange er offen steht.</para>
/// </summary>
public sealed class NutzungsdauerKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? SucheLesen { get; init; }
    public Action<string>? SucheSetzen { get; init; }

    public Func<int?>? TechnikLesen { get; init; }
    public Action<int?>? TechnikSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? TechnikEintraege { get; init; }

    public Func<string>? ArtLesen { get; init; }
    public Action<string>? ArtSetzen { get; init; }

    public Func<double?>? WertLesen { get; init; }
    public Action<double?>? WertSetzen { get; init; }

    public Func<double?>? AfaLesen { get; init; }
    public Action<double?>? AfaSetzen { get; init; }

    // ETAPPE E10 (Stufe S3): die zwei Sätze der Neuzeile.
    public Func<double?>? InstandsetzungLesen { get; init; }
    public Action<double?>? InstandsetzungSetzen { get; init; }

    public Func<double?>? WartungLesen { get; init; }
    public Action<double?>? WartungSetzen { get; init; }

    /// <summary>Die lebenden Zeilen des Dialogs — die Spalten lösen über sie auf.</summary>
    public Func<IReadOnlyList<NutzungsdauerZeileAnzeige>>? ZeilenLesen { get; init; }

    /// <summary>Die Techniken der Klappliste (KI‑D‑Q6) — Schlüssel ist ihre Id.</summary>
    public IReadOnlyList<KiWahleintrag> NeueTechnikWahl
        => TechnikEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der Filtertext über der Tabelle.</summary>
    public string Suche
    {
        get => SucheLesen?.Invoke() ?? "";
        set => SucheSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Technik der Neuzeile; leer = technikübergreifend.</summary>
    public int? NeueTechnik
    {
        get => TechnikLesen?.Invoke();
        set => TechnikSetzen?.Invoke(value);
    }

    /// <summary>Die Positionsart der Neuzeile.</summary>
    public string NeuePositionsart
    {
        get => ArtLesen?.Invoke() ?? "";
        set => ArtSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Nutzungsdauer der Neuzeile.</summary>
    public double? NeueNutzungsdauer
    {
        get => WertLesen?.Invoke();
        set => WertSetzen?.Invoke(value);
    }

    /// <summary>Die steuerliche AfA-Dauer der Neuzeile.</summary>
    public double? NeueAfa
    {
        get => AfaLesen?.Invoke();
        set => AfaSetzen?.Invoke(value);
    }

    /// <summary>ETAPPE E10: der Instandsetzungssatz der Neuzeile [%/a].</summary>
    public double? NeueInstandsetzung
    {
        get => InstandsetzungLesen?.Invoke();
        set => InstandsetzungSetzen?.Invoke(value);
    }

    /// <summary>ETAPPE E10: der Wartungssatz der Neuzeile [%/a].</summary>
    public double? NeueWartung
    {
        get => WartungLesen?.Invoke();
        set => WartungSetzen?.Invoke(value);
    }

    /// <summary>Die Zeilen der Tabelle — Grundlage der Spaltenfelder.</summary>
    public IReadOnlyList<NutzungsdauerZeileAnzeige> Zeilen
        => ZeilenLesen?.Invoke() ?? Array.Empty<NutzungsdauerZeileAnzeige>();
}
