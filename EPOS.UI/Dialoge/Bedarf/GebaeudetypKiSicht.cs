using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild der Gebäudetypen-Verwaltung für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum ein Sichtmodell und nicht <c>GebaeudetypDaten</c>.</b> Der NAME dieser
/// Maske ist keine Eingabe, sondern eine WAHL: Die Liste links wählt den Typ, und mit
/// ihm lädt die Maske einen anderen Satz samt seinen Tagesverteilungen. Ein Setzen von
/// <c>Daten.Name</c> benannte bloß den Satz im Speicher um; die Maske zeigte weiterhin
/// die Kurven des vorigen. Dieselbe Lage bei der gewählten KURVE — sie ist eine
/// Listenwahl der Komponente, kein Feld des Satzes.</para>
///
/// <para><b>Die 24 Stundenwerte der GEWÄHLTEN Kurve sind EINE Zahlenreihe</b> (Welle #458
/// Stufe 3b, <see cref="Stundenwerte"/>): derselbe Arbeitsstand, in den „Übernehmen" der
/// Überlagerung „Stundenwerte…" schreibt. Geschrieben wird mit „Speichern"; solange
/// geänderte Werte ungespeichert sind, hält die Maske den Kurvenwechsel an.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class GebaeudetypKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? TypLesen { get; init; }
    public Action<string>? TypSetzen { get; init; }

    public Func<int?>? KurveLesen { get; init; }
    public Action<int?>? KurveSetzen { get; init; }

    public Func<string>? BeschreibungLesen { get; init; }

    /// <summary>
    /// Setzt die Beschreibung im Arbeitsstand — derselbe Weg wie eine Eingabe von Hand
    /// (<c>BeiBeschreibung</c>); gespeichert wird mit „Speichern".
    /// </summary>
    public Action<string>? BeschreibungSetzen { get; init; }

    /// <summary>Liest die 24 Stundenwerte der gewählten Kurve; <c>null</c> = keine Kurve.</summary>
    public Func<double?[]?>? StundenwerteLesen { get; init; }

    /// <summary>
    /// Legt die 24 Stundenwerte der gewählten Kurve in den Arbeitsstand; ohne Kurve oder
    /// an einem Auslieferungstyp lehnt die Maske mit Grund ab.
    /// </summary>
    public Action<double?[]>? StundenwerteSetzen { get; init; }

    /// <summary>Liefert die Gebäudetypen, die die Liste der Maske führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? TypEintraege { get; init; }

    /// <summary>Liefert die Kurvennamen des geladenen Typs (fünf oder acht).</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? KurveEintraege { get; init; }

    /// <summary>Die Gebäudetypen des Katalogs (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> TypWahl
        => TypEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Tageskurven des geladenen Typs (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> KurveWahl
        => KurveEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Der geladene Gebäudetyp. Ein Setzen LÄDT den Typ — denselben Weg nimmt ein
    /// Klick in die Liste.
    /// </summary>
    public string Typ
    {
        get => TypLesen?.Invoke() ?? "";
        set => TypSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Die Tageskurve, deren 24 Stundenwerte die Maske zeigt; der Schlüssel ist ihr
    /// Listenplatz.
    /// </summary>
    public int? Kurve
    {
        get => KurveLesen?.Invoke();
        set => KurveSetzen?.Invoke(value);
    }

    /// <summary>
    /// Der Freitext des geladenen Typs — im Arbeitsstand bearbeitbar wie das Feld der
    /// Maske. Einen Auslieferungstyp schützt der Haken <c>Schreibgeschuetzt</c> der Maske,
    /// und die Maske selbst übernimmt dort keine Eingabe.
    /// </summary>
    public string Beschreibung
    {
        get => BeschreibungLesen?.Invoke() ?? "";
        set => BeschreibungSetzen?.Invoke(value ?? "");
    }

    /// <summary>
    /// Die Zahlenreihe der 24 Stundenwerte der gewählten Kurve (Welle #458 Stufe 3b),
    /// Stunde 1 bis 24; <c>null</c>, solange kein Typ mit Kurven geladen ist.
    /// </summary>
    public double?[]? Stundenwerte
    {
        get => StundenwerteLesen?.Invoke();
        set { if (value is not null) StundenwerteSetzen?.Invoke(value); }
    }
}
