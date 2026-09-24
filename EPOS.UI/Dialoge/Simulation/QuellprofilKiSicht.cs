using KiKern;

namespace EPOS.UI.Dialoge.Simulation;

/// <summary>
/// Das FLACHE Abbild der Maske „Quellprofil" für den Hilfe-Assistenten (Welle KI‑F2).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>QuellprofilDaten</c>:</b> Jener Satz ist
/// ein unveränderlicher Record und trägt ohnehin nur die VORAUSWAHL (Projekt, Profil-Id
/// und die zwei Altwertreihen); was der Anwender bearbeitet, steht in den Feldern der
/// Maske. Diese Klasse legt sich über sie.</para>
///
/// <para><b>Der KOPF und die zwölf Monatswerte.</b> Die Monatswerte sind EINE
/// Zahlenreihe (Welle #458 Stufe 3b, <see cref="Monatswerte"/>) — in der Betriebsart
/// „Monat", in der die Maske sie zeigt; sonst liest die Reihe leer, und das Setzen lehnt
/// mit Grund ab. Die 365 bzw. 8 760 Werte der Betriebsarten Tag und Stunde bleiben
/// draußen: Sie sind Zeitreihen, gepflegt über „Alle Werte gleich setzen…" und den
/// CSV-Weg.</para>
/// </summary>
public sealed class QuellprofilKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? BezeichnungLesen { get; init; }
    public Action<string>? BezeichnungSetzen { get; init; }

    public Func<string>? BeschreibungLesen { get; init; }
    public Action<string>? BeschreibungSetzen { get; init; }

    public Func<string>? BetriebsartLesen { get; init; }
    public Action<string>? BetriebsartSetzen { get; init; }

    public Func<int?>? ProfilLesen { get; init; }
    public Action<int?>? ProfilSetzen { get; init; }

    /// <summary>Liest die zwölf Monatswerte; <c>null</c> außerhalb der Betriebsart „Monat".</summary>
    public Func<double?[]?>? MonatswerteLesen { get; init; }

    /// <summary>Schreibt die zwölf Monatswerte; außerhalb der Betriebsart „Monat" mit Grund abgelehnt.</summary>
    public Action<double?[]>? MonatswerteSetzen { get; init; }

    // =====================================================================
    //  Die Einträge der beiden Wahlfelder (KI-F1b)
    // =====================================================================

    /// <summary>Liefert die Betriebsarten, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? BetriebsartEintraege { get; init; }

    /// <summary>Liefert die Profile, die die Maske zur Wahl stellt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? ProfilEintraege { get; init; }

    /// <summary>Monat, Tag und Stunde — die Auswahl der Maske (KI-F1b).</summary>
    public IReadOnlyList<KiWahleintrag> BetriebsartWahl
        => BetriebsartEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>
    /// Die Profile des Projekts samt dem Eintrag „neues Profil" — die Auswahl der
    /// Maske (KI-F1b).
    /// </summary>
    public IReadOnlyList<KiWahleintrag> ProfilWahl
        => ProfilEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>Der Name des Profils — Pflichtangabe beim Speichern.</summary>
    public string Bezeichnung
    {
        get => BezeichnungLesen?.Invoke() ?? "";
        set => BezeichnungSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Freitext zum Profil.</summary>
    public string Beschreibung
    {
        get => BeschreibungLesen?.Invoke() ?? "";
        set => BeschreibungSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Der Steuerwert der Betriebsart — Monat, Tag oder Stunde; er entscheidet, wie
    /// viele Werte das Profil trägt (12, 365 oder 8 760).
    /// </summary>
    /// <remarks>
    /// <b>Ein unbekannter Steuerwert wird abgewiesen</b>, statt die Maske auf ein
    /// Raster zu stellen, das es nicht gibt: Der Dialog kennt genau die Steuerwerte
    /// seiner Klappliste.
    /// </remarks>
    public string Betriebsart
    {
        get => BetriebsartLesen?.Invoke() ?? "";
        set => BetriebsartSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Die Id des gewählten Profils; <c>0</c> ist der Eintrag „neues Profil". Die
    /// Wahl LÄDT den Profilkopf samt Werten — sie ist derselbe Weg wie die Klappliste.
    /// </summary>
    public int? Profil
    {
        get => ProfilLesen?.Invoke();
        set => ProfilSetzen?.Invoke(value);
    }

    /// <summary>
    /// Die Zahlenreihe der zwölf Monats-Mitteltemperaturen [°C] (Welle #458 Stufe 3b),
    /// Januar bis Dezember.
    /// </summary>
    public double?[]? Monatswerte
    {
        get => MonatswerteLesen?.Invoke();
        set { if (value is not null) MonatswerteSetzen?.Invoke(value); }
    }
}
