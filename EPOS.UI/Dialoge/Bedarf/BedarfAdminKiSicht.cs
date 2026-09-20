using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild einer Bedarfs-Katalogverwaltung für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum diese Maske trotz KI‑D‑Q5 im Katalog steht.</b> Sie ist mehr als eine
/// Liste mit Suchfeld: Neben der Katalogliste führt sie einen Infoblock, der Typ,
/// Beschreibung und Jahressumme des markierten Satzes nennt — und genau danach fragt
/// der Anwender. Die Liste selbst ist ein WAHLFELD; setzen heißt hier markieren, und
/// der Infoblock zieht nach.</para>
///
/// <para><b>EINE Komponente, DREI Katalogschlüssel.</b> Prozesswärme,
/// Stromverbraucher und Brauchwasser sind drei Masken mit eigenem Namen und eigenem
/// Navigationsziel; gezeichnet wird dieselbe Komponente, und welche Ausprägung offen
/// ist, sagt das Feld <c>bedarfsart</c>.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft ruft bei jedem Zugriff ihren
/// Delegaten.</para>
/// </summary>
public sealed class BedarfAdminKiSicht
{
    // =====================================================================
    //  Die Zugriffswege — der Dialog setzt sie beim Anmelden
    // =====================================================================

    public Func<string>? SatzLesen { get; init; }
    public Action<string>? SatzSetzen { get; init; }

    public Func<string>? BeschreibungLesen { get; init; }
    public Func<string>? TypLesen { get; init; }
    public Func<string>? JahressummeLesen { get; init; }
    public Func<string>? BedarfsartLesen { get; init; }

    /// <summary>Liefert die Katalogsätze, die die Liste der Maske führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    /// <summary>Die Sätze des Katalogs (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> SatzWahl
        => SatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Der markierte Katalogsatz. Ihn zu setzen MARKIERT ihn in der Liste — derselbe
    /// Weg wie ein Klick; der Infoblock zieht nach.
    /// </summary>
    public string Satz
    {
        get => SatzLesen?.Invoke() ?? "";
        set => SatzSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Beschreibung des markierten Satzes.</summary>
    public string Beschreibung => BeschreibungLesen?.Invoke() ?? "";

    /// <summary>Der Typ des markierten Satzes aus seinem Kopfsatz.</summary>
    public string Typ => TypLesen?.Invoke() ?? "";

    /// <summary>Die Jahressumme des markierten Satzes, wie sie dasteht.</summary>
    public string Jahressumme => JahressummeLesen?.Invoke() ?? "";

    /// <summary>
    /// Welche Ausprägung offen ist: Prozesswärme, Stromverbraucher oder Brauchwasser.
    /// </summary>
    public string Bedarfsart => BedarfsartLesen?.Invoke() ?? "";
}
