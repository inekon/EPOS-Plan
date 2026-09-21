using KiKern;

namespace EPOS.UI.Dialoge.Strom;

/// <summary>
/// Das FLACHE Abbild der Stromganglinien-Verwaltung für den Hilfe-Assistenten
/// (Welle KI‑F6).
///
/// <para><b>Diese Maske führt genau EINEN Einstellwert</b>: das Zeitintervall,
/// mit dem eine eingelesene Datei abgelegt wird. Alles andere darauf ist Suche,
/// Auswahl und Anzeige — die virtualisierte Katalogliste samt Filterstand, die
/// gerechneten Spalten Jahresarbeit und Spitze, der Löschweg und die
/// Importkette. Nach KI‑D‑Q5 bleibt beides draußen: eine Menge von Verweisen
/// ist kein Feldwert, und eine Datei einzulesen ist ein Ladevorgang.</para>
///
/// <para><b>Freigegeben ist sie trotzdem</b>, damit <c>dialog_lesen</c> nennt,
/// woran der Anwender gerade arbeitet, und <c>feld_setzen</c> benannt ablehnt
/// statt „Maske nicht freigegeben" — dieselbe Begründung wie beim Reiterblatt
/// „Kosten" der Welle KI‑F4 und bei der Leistungspreisreihe.</para>
///
/// <para><b>Der gewählte Katalogsatz steht als ANZEIGE daneben.</b> Ihn zu
/// setzen hieße, in einer Liste zu blättern, die nach einem Import eine andere
/// ist; der Assistent nennt ihn, wählt ihn aber nicht.</para>
/// </summary>
public sealed class StromganglinieAdminKiSicht
{
    /// <summary>Liest den Platz des gewählten Zeitintervalls.</summary>
    public Func<int?>? ZeitintervallLesen { get; init; }

    /// <summary>Setzt das Zeitintervall — derselbe Weg wie die Klappliste.</summary>
    public Action<int?>? ZeitintervallSetzen { get; init; }

    /// <summary>Die zwei Einträge der Klappliste (Stunde, Viertelstunde).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? ZeitintervallEintraege { get; init; }

    /// <summary>Liest den Bezeichner der markierten Katalogzeile.</summary>
    public Func<string>? GewaehltLesen { get; init; }

    /// <summary>Liest den Text des Warnbanners.</summary>
    public Func<string>? MeldungLesen { get; init; }

    /// <summary>Die Einträge der Klappliste „Zeitinterval".</summary>
    public IReadOnlyList<KiWahleintrag> ZeitintervallWahl
        => ZeitintervallEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>
    /// Das Zeitintervall, mit dem eine eingelesene Datei abgelegt wird.
    /// </summary>
    public int? Zeitintervall
    {
        get => ZeitintervallLesen?.Invoke();
        set => ZeitintervallSetzen?.Invoke(value);
    }

    /// <summary>Die markierte Katalogzeile — Anzeige.</summary>
    public string Gewaehlt => GewaehltLesen?.Invoke() ?? "";

    /// <summary>Der Befund der Maske — Anzeige.</summary>
    public string Meldung => MeldungLesen?.Invoke() ?? "";
}
