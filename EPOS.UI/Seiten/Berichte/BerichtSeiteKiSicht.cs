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
///
/// <para><b>BV-E1 (Konzept 10.2): die Gruppe „Vorlage".</b> Die Vorlagenwahl ist ein
/// Wahlfeld (<see cref="Vorlage"/> mit <see cref="VorlageWahl"/>) — gesetzt wird sie über
/// denselben Weg wie das Auswahlfeld, ein gesperrter Eintrag und ein laufender Bericht
/// lehnen benannt ab. Die Prüfzeile ist eine Anzeige, die Suche des Platzhalterkatalogs
/// (<see cref="Katalogsuche"/>) ein Einstellwert: Der Katalog steht als Überlagerung IN
/// dieser Seite, und der Wirt meldet an, nicht das Blatt darin. Die Feldkarte des Kerns
/// (<c>KiDialoge</c>, Maske Berichtsseite) zieht die drei Felder nach.</para>
/// </summary>
public sealed class BerichtSeiteKiSicht
{
    /// <summary>Liest die gewählte Word-Vorlage (<c>Vorlagenzeile.Id</c>); <c>null</c> = keine.</summary>
    public Func<int?>? VorlageLesen { get; init; }

    /// <summary>Setzt die Word-Vorlage — derselbe Weg wie das Auswahlfeld; wirft mit Grund, wo es nicht geht.</summary>
    public Action<int?>? VorlageSetzen { get; init; }

    /// <summary>Die Vorlagen der Liste als Wahleinträge.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? VorlageEintraege { get; init; }

    /// <summary>Liest die Prüfzeile als Text.</summary>
    public Func<string>? PruefzeileLesen { get; init; }

    /// <summary>BV-E7: liest die gewählte Excel-Vorlage (<c>Vorlagenzeile.Id</c>); <c>null</c> = keine.</summary>
    public Func<int?>? ExcelVorlageLesen { get; init; }

    /// <summary>BV-E7: setzt die Excel-Vorlage — derselbe Weg wie das Auswahlfeld; wirft mit Grund, wo es nicht geht.</summary>
    public Action<int?>? ExcelVorlageSetzen { get; init; }

    /// <summary>BV-E7: die Excel-Vorlagen der Liste als Wahleinträge.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? ExcelVorlageEintraege { get; init; }

    /// <summary>BV-E7: liest die Prüfzeile der Excel-Vorlage als Text.</summary>
    public Func<string>? ExcelPruefzeileLesen { get; init; }

    /// <summary>Liest die Suche des Platzhalterkatalogs.</summary>
    public Func<string>? KatalogsucheLesen { get; init; }

    /// <summary>Setzt die Suche des Platzhalterkatalogs.</summary>
    public Action<string>? KatalogsucheSetzen { get; init; }

    /// <summary>Die Vorlagen der Liste.</summary>
    public IReadOnlyList<KiWahleintrag> VorlageWahl
        => VorlageEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Mit welcher Word-Vorlage der Bericht entsteht.</summary>
    public int? Vorlage
    {
        get => VorlageLesen?.Invoke();
        set => VorlageSetzen?.Invoke(value);
    }

    /// <summary>Was die Prüfung der gewählten Vorlage sagt — Anzeige.</summary>
    public string Pruefzeile => PruefzeileLesen?.Invoke() ?? "";

    /// <summary>BV-E7: die Excel-Vorlagen der Liste.</summary>
    public IReadOnlyList<KiWahleintrag> ExcelVorlageWahl
        => ExcelVorlageEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>BV-E7: aus welcher Excel-Vorlage die Mappe entsteht („Ohne Vorlage“ = die Mappe aus dem Code).</summary>
    public int? ExcelVorlage
    {
        get => ExcelVorlageLesen?.Invoke();
        set => ExcelVorlageSetzen?.Invoke(value);
    }

    /// <summary>BV-E7: was die Prüfung der Excel-Vorlage sagt — Anzeige.</summary>
    public string ExcelPruefzeile => ExcelPruefzeileLesen?.Invoke() ?? "";

    /// <summary>Wonach der Platzhalterkatalog filtert (Schlüssel und Beschreibung).</summary>
    public string Katalogsuche
    {
        get => KatalogsucheLesen?.Invoke() ?? "";
        set => KatalogsucheSetzen?.Invoke(value ?? "");
    }

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
