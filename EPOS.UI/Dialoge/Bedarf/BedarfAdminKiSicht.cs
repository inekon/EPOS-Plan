using KiKern;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// Das FLACHE Abbild einer Bedarfs-Katalogverwaltung für den Hilfe-Assistenten
/// (Welle KI‑F3).
///
/// <para><b>Warum diese Maske trotz KI‑D‑Q5 im Katalog steht.</b> Sie ist mehr als eine
/// Liste mit Suchfeld: Neben der Katalogliste führt sie ein Stammblatt mit Typ,
/// Beschreibung, Jahressumme und den zwölf Monatswerten des markierten Satzes — und genau
/// danach fragt der Anwender. Die Liste selbst ist ein WAHLFELD; setzen heißt hier
/// markieren, und das Stammblatt zieht nach.</para>
///
/// <para><b>Die Kenndaten sind Eingaben</b> (Welle #456): Typ, Beschreibung und die zwölf
/// Monatswerte schreiben in den ARBEITSSTAND des Dialogs — denselben, den die Felder des
/// Stammblatts bedienen; „Speichern" schreibt ihn. Ob der Satz geschützt ist, sagt der
/// Haken des Dialogs, nicht diese Klasse.</para>
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

    /// <summary>
    /// Wählt einen Satz; Rückgabe: der Grund, warum der Dialog den Wechsel ablehnt, sonst
    /// <c>null</c>.
    /// </summary>
    public Func<string, string?>? SatzSetzen { get; init; }

    public Func<string>? BeschreibungLesen { get; init; }
    public Action<string>? BeschreibungSetzen { get; init; }
    public Func<string>? TypLesen { get; init; }
    public Action<string>? TypSetzen { get; init; }
    public Func<string>? JahressummeLesen { get; init; }
    public Func<string>? BedarfsartLesen { get; init; }

    /// <summary>Liest den Monatswert 0–11 des Arbeitsstands.</summary>
    public Func<int, double?>? MonatLesen { get; init; }

    /// <summary>Setzt den Monatswert 0–11 des Arbeitsstands.</summary>
    public Action<int, double?>? MonatSetzen { get; init; }

    /// <summary>Liefert die Katalogsätze, die die Liste der Maske führt.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? SatzEintraege { get; init; }

    /// <summary>Liefert die Typen der Klappliste.</summary>
    public Func<IReadOnlyList<KiWahleintrag>>? TypEintraege { get; init; }

    /// <summary>Die Sätze des Katalogs (KI‑D‑Q6).</summary>
    public IReadOnlyList<KiWahleintrag> SatzWahl
        => SatzEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    /// <summary>Die Typen der Klappliste (KI‑D‑Q6) — Schlüssel ist der Typname.</summary>
    public IReadOnlyList<KiWahleintrag> TypWahl
        => TypEintraege?.Invoke() ?? Array.Empty<KiWahleintrag>();

    // =====================================================================
    //  Die Felder der Maske
    // =====================================================================

    /// <summary>
    /// Der markierte Katalogsatz. Ihn zu setzen MARKIERT ihn in der Liste — derselbe
    /// Weg wie ein Klick; das Stammblatt zieht nach. Ein abgelehnter Wechsel
    /// (ungespeicherte Änderungen) kommt als benannte Ausnahme zurück.
    /// </summary>
    public string Satz
    {
        get => SatzLesen?.Invoke() ?? "";
        set
        {
            string? grund = SatzSetzen?.Invoke(value ?? "");
            if (!string.IsNullOrEmpty(grund)) throw new InvalidOperationException(grund);
        }
    }

    /// <summary>Die Beschreibung des markierten Satzes (Arbeitsstand).</summary>
    public string Beschreibung
    {
        get => BeschreibungLesen?.Invoke() ?? "";
        set => BeschreibungSetzen?.Invoke(value ?? "");
    }

    /// <summary>Der Typ des markierten Satzes (Arbeitsstand), gewählt aus der Typliste.</summary>
    public string Typ
    {
        get => TypLesen?.Invoke() ?? "";
        set => TypSetzen?.Invoke(value ?? "");
    }

    /// <summary>Die Jahressumme des markierten Satzes, wie sie dasteht.</summary>
    public string Jahressumme => JahressummeLesen?.Invoke() ?? "";

    /// <summary>
    /// Welche Ausprägung offen ist: Prozesswärme, Stromverbraucher oder Brauchwasser.
    /// </summary>
    public string Bedarfsart => BedarfsartLesen?.Invoke() ?? "";

    // ---- Die zwölf Monatswerte (Welle #456) -------------------------------

    public double? Januar { get => Monat(0); set => Monat(0, value); }
    public double? Februar { get => Monat(1); set => Monat(1, value); }
    public double? Maerz { get => Monat(2); set => Monat(2, value); }
    public double? April { get => Monat(3); set => Monat(3, value); }
    public double? Mai { get => Monat(4); set => Monat(4, value); }
    public double? Juni { get => Monat(5); set => Monat(5, value); }
    public double? Juli { get => Monat(6); set => Monat(6, value); }
    public double? August { get => Monat(7); set => Monat(7, value); }
    public double? September { get => Monat(8); set => Monat(8, value); }
    public double? Oktober { get => Monat(9); set => Monat(9, value); }
    public double? November { get => Monat(10); set => Monat(10, value); }
    public double? Dezember { get => Monat(11); set => Monat(11, value); }

    private double? Monat(int m) => MonatLesen?.Invoke(m);

    private void Monat(int m, double? wert) => MonatSetzen?.Invoke(m, wert);
}
