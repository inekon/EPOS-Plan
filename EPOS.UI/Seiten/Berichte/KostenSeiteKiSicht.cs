using KiKern;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Das FLACHE Abbild des Reiterblatts „Kosten" für den Hilfe-Assistenten
/// (Welle KI‑F4).
///
/// <para><b>Diese Seite führt genau EINEN Einstellwert</b>: die markierte Anlage.
/// Sie entscheidet, welche Komponente die Kostenverwaltung öffnet und welche
/// Energieträgerzeile hervorgehoben steht. Alles andere — die drei Kennzahlkarten,
/// die Gegenüberstellung der Versionen, die Komponenten- und die Trägertabelle —
/// ist gerechnete ANZEIGE.</para>
///
/// <para><b>Freigegeben ist sie trotzdem</b>, damit <c>dialog_lesen</c> nennt,
/// woran der Anwender gerade arbeitet, und <c>feld_setzen</c> benannt ablehnt statt
/// „Maske nicht freigegeben" — dieselbe Begründung wie bei der Solarganglinienmaske
/// der Welle KI‑F3.</para>
///
/// <para><b>Die VERGLEICHSWAHL bleibt draußen.</b> Sie ist eine Menge von Verweisen
/// (die Ids der angehakten Varianten) und kein Feldwert; ein Maskenfeld trägt genau
/// einen Wert (<c>KiDialogFeld</c> lehnt eine Zahlenliste ab).</para>
/// </summary>
public sealed class KostenSeiteKiSicht
{
    public Func<int?>? AnlageLesen { get; init; }
    public Action<int?>? AnlageSetzen { get; init; }
    public Func<IReadOnlyList<KiWahleintrag>>? AnlagenEintraege { get; init; }

    public Func<string>? ProjektzeileLesen { get; init; }
    public Func<string>? StatuszeileLesen { get; init; }

    /// <summary>Die wählbaren Komponenten der Kostentabelle — Schlüssel ist ihr Satz.</summary>
    public IReadOnlyList<KiWahleintrag> AnlageWahl
        => AnlagenEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die in der Kostentabelle markierte Anlage.</summary>
    public int? Anlage
    {
        get => AnlageLesen?.Invoke();
        set => AnlageSetzen?.Invoke(value);
    }

    /// <summary>Das Projekt, dessen Kosten die Seite zeigt — Anzeige.</summary>
    public string Projektzeile => ProjektzeileLesen?.Invoke() ?? "";

    /// <summary>Die Fußzeile mit den Befunden der Seite — Anzeige.</summary>
    public string Statuszeile => StatuszeileLesen?.Invoke() ?? "";
}
