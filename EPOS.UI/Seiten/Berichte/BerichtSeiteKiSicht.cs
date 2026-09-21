using KiKern;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// Das FLACHE Abbild des Reiterblatts „Bericht" für den Hilfe-Assistenten
/// (Welle KI‑F6).
///
/// <para><b>Diese Seite führt ZWEI Einstellwerte</b>: das Ausgabeformat und den
/// Zielordner. Beide stehen im Stand der Seite und gehen mit dem Berichtslauf in
/// die Hülle.</para>
///
/// <para><b>Die zwei MENGEN bleiben lesbar.</b> Welche Versionen in den Bericht
/// gehen und welche Bausteine er trägt, sind Mengen von Verweisen und keine
/// Feldwerte (<c>KiDialogFeld</c> trägt genau einen); sie gehen als AUFSTELLUNG
/// hinaus — genau die Zeile, die die Maske zeigt. Dieselbe Bauart wie bei der
/// Einheitenliste der Stromspeicher-Ansicht und dieselbe Regel wie bei der
/// Vergleichswahl des Reiterblatts „Kosten".</para>
///
/// <para><b>Kein Speicherweg.</b> „Erstellen" rechnet und schreibt eine Datei —
/// eine Aktion der Stufen 2 und 3 mit eigener Rückfrage, kein Speichern der
/// Maske.</para>
/// </summary>
public sealed class BerichtSeiteKiSicht
{
    /// <summary>Liest das gewählte Ausgabeformat (0 = Word, 1 = Excel, 2 = beide).</summary>
    public Func<int?>? AusgabeLesen { get; init; }

    /// <summary>Setzt das Ausgabeformat — derselbe Weg wie die Optionsgruppe.</summary>
    public Action<int?>? AusgabeSetzen { get; init; }

    /// <summary>Die drei Ausgabeformen der Maske.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? AusgabeEintraege { get; init; }

    /// <summary>Liest den Zielordner.</summary>
    public Func<string>? ZielordnerLesen { get; init; }

    /// <summary>Schreibt den Zielordner.</summary>
    public Action<string>? ZielordnerSetzen { get; init; }

    /// <summary>Liest die angehakten Versionen als Aufstellung.</summary>
    public Func<string>? VariantenLesen { get; init; }

    /// <summary>Liest die gewählten Bausteine als Aufstellung.</summary>
    public Func<string>? BausteineLesen { get; init; }

    /// <summary>Liest die Statuszeile der Seite.</summary>
    public Func<string>? StatusLesen { get; init; }

    /// <summary>Die drei Ausgabeformen.</summary>
    public IReadOnlyList<KiWahleintrag> AusgabeWahl
        => AusgabeEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>In welchem Format der Bericht entsteht.</summary>
    public int? Ausgabe
    {
        get => AusgabeLesen?.Invoke();
        set => AusgabeSetzen?.Invoke(value);
    }

    /// <summary>Der Ordner, in den der Bericht geschrieben wird.</summary>
    public string Zielordner
    {
        get => ZielordnerLesen?.Invoke() ?? "";
        set => ZielordnerSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Versionen, die in den Bericht gehen — Anzeige.</summary>
    public string Varianten => VariantenLesen?.Invoke() ?? "";

    /// <summary>Die Bausteine, die der Bericht trägt — Anzeige.</summary>
    public string Bausteine => BausteineLesen?.Invoke() ?? "";

    /// <summary>Die Statuszeile der Seite — Anzeige.</summary>
    public string Statuszeile => StatusLesen?.Invoke() ?? "";
}
