using KiKern;

namespace EPOS.UI.Dialoge.Projekt;

/// <summary>
/// Das FLACHE Abbild der Maske „Projekt speichern unter" für den
/// Hilfe-Assistenten (Welle KI‑F6).
///
/// <para><b>Warum eine Sichtklasse und kein Daten-Objekt.</b> Der Dialog führt
/// seinen Stand in sieben privaten Feldern der Komponente; ein DTO entsteht erst
/// im OK-Weg und ist danach fort — dieselbe Lage wie bei den Masken der
/// Simulationskonfiguration (Welle KI‑F2).</para>
///
/// <para><b>Das QUELLPROJEKT ist ein Wahlfeld</b> (KI‑D‑Q6) und trägt als
/// Schlüssel seine Projekt-Id. Es zu setzen belegt Beschreibung, Kunde und
/// Bearbeiter aus dem Quellprojekt vor — genau wie ein Klick in die Liste; das
/// ist die Absicht der Maske und keine Nebenwirkung.</para>
///
/// <para><b>Die SUCHE bleibt draußen.</b> Sie schränkt die Projektliste ein und
/// ist damit Teil einer Menge von Verweisen, kein Einstellwert der Maske —
/// dieselbe Regel wie beim Filterstand der Katalogliste.</para>
/// </summary>
public sealed class ProjektKopieKiSicht
{
    /// <summary>Liest die markierte Projektzeile.</summary>
    public Func<int?>? QuelleLesen { get; init; }

    /// <summary>Markiert ein Quellprojekt — derselbe Weg wie ein Klick in die Liste.</summary>
    public Action<int?>? QuelleSetzen { get; init; }

    /// <summary>Die Projekte, die die Liste führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? QuelleEintraege { get; init; }

    /// <summary>Liest den neuen Projektnamen.</summary>
    public Func<string>? NameLesen { get; init; }

    /// <summary>Schreibt den neuen Projektnamen.</summary>
    public Action<string>? NameSetzen { get; init; }

    /// <summary>Liest die Beschreibung.</summary>
    public Func<string>? BeschreibungLesen { get; init; }

    /// <summary>Schreibt die Beschreibung.</summary>
    public Action<string>? BeschreibungSetzen { get; init; }

    /// <summary>Liest den Kunden.</summary>
    public Func<string>? KundeLesen { get; init; }

    /// <summary>Schreibt den Kunden.</summary>
    public Action<string>? KundeSetzen { get; init; }

    /// <summary>Liest den Bearbeiter.</summary>
    public Func<string>? BearbeiterLesen { get; init; }

    /// <summary>Schreibt den Bearbeiter.</summary>
    public Action<string>? BearbeiterSetzen { get; init; }

    /// <summary>Die wählbaren Quellprojekte.</summary>
    public IReadOnlyList<KiWahleintrag> QuellprojektWahl
        => QuelleEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Das Projekt, das kopiert wird.</summary>
    public int? Quellprojekt
    {
        get => QuelleLesen?.Invoke();
        set => QuelleSetzen?.Invoke(value);
    }

    /// <summary>Der Name der Kopie.</summary>
    public string NeuerName
    {
        get => NameLesen?.Invoke() ?? "";
        set => NameSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Beschreibung der Kopie.</summary>
    public string Beschreibung
    {
        get => BeschreibungLesen?.Invoke() ?? "";
        set => BeschreibungSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Kunde der Kopie.</summary>
    public string Kunde
    {
        get => KundeLesen?.Invoke() ?? "";
        set => KundeSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Bearbeiter der Kopie.</summary>
    public string Bearbeiter
    {
        get => BearbeiterLesen?.Invoke() ?? "";
        set => BearbeiterSetzen?.Invoke(value ?? "");
    }
}

/// <summary>
/// Das FLACHE Abbild der Maske „Als Variante speichern" für den
/// Hilfe-Assistenten (Welle KI‑F6).
///
/// <para><b>Drei Einstellwerte und eine Anzeige.</b> Der Haken entscheidet, ob
/// der Inhalt eines bestehenden Projekts übernommen wird; nur dann steht die
/// Projektliste darunter. Der ZIELNAME ist gerechnet
/// (<c>VariantenCtrl.Zielname</c> samt Kollisionszähler) und deshalb nur
/// lesbar.</para>
///
/// <para><b>Der Bezeichner folgt dem Quellprojekt, bis er von Hand getippt
/// wird.</b> Die Sicht setzt ihn über denselben Weg wie das Eingabefeld — damit
/// gilt dieselbe Regel: Ein vom Assistenten gesetzter Bezeichner bleibt stehen,
/// auch wenn danach ein anderes Quellprojekt gewählt wird.</para>
/// </summary>
public sealed class ProjektVarianteKiSicht
{
    /// <summary>Liest den Haken „Inhalt aus einem bestehenden Projekt übernehmen".</summary>
    public Func<bool>? HakenLesen { get; init; }

    /// <summary>Setzt den Haken — derselbe Weg wie ein Klick darauf.</summary>
    public Action<bool>? HakenSetzen { get; init; }

    /// <summary>Liest das gewählte Quellprojekt.</summary>
    public Func<int?>? QuelleLesen { get; init; }

    /// <summary>Wählt ein Quellprojekt — derselbe Weg wie ein Klick in die Liste.</summary>
    public Action<int?>? QuelleSetzen { get; init; }

    /// <summary>Die Projekte, die die Liste führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? QuelleEintraege { get; init; }

    /// <summary>Liest den Bezeichner der neuen Variante.</summary>
    public Func<string>? BezeichnerLesen { get; init; }

    /// <summary>Schreibt den Bezeichner der neuen Variante.</summary>
    public Action<string>? BezeichnerSetzen { get; init; }

    /// <summary>Liest die Vorschauzeile mit dem Zielnamen.</summary>
    public Func<string>? ZielnameLesen { get; init; }

    /// <summary>Wird der Inhalt eines bestehenden Projekts übernommen?</summary>
    public bool AusQuelle
    {
        get => HakenLesen?.Invoke() ?? false;
        set => HakenSetzen?.Invoke(value);
    }

    /// <summary>Die wählbaren Quellprojekte.</summary>
    public IReadOnlyList<KiWahleintrag> QuellprojektWahl
        => QuelleEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Das Projekt, dessen Inhalt die Variante bekommt.</summary>
    public int? Quellprojekt
    {
        get => QuelleLesen?.Invoke();
        set => QuelleSetzen?.Invoke(value);
    }

    /// <summary>Der Bezeichner der neuen Variante.</summary>
    public string Bezeichner
    {
        get => BezeichnerLesen?.Invoke() ?? "";
        set => BezeichnerSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Name, unter dem die Variante entsteht — Anzeige.</summary>
    public string Zielname => ZielnameLesen?.Invoke() ?? "";
}
