using Microsoft.AspNetCore.Components;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Eine Handlung der Auswahlleiste</b> (Konzept Administrationsdialoge, Stufe 3,
/// Vorschlag V8) — als DATEN: Beschriftung, Rückruf, kleinste und größte Zeilenzahl und
/// der Grund, aus dem sie gerade nicht geht.
///
/// <para><b>Kein Rückruf, kein Knopf</b> (Hausregel): Eine Handlung ohne
/// <see cref="Ausfuehren"/> zeichnet die Leiste nicht. Der Rückruf ist ein
/// <see cref="EventCallback"/>, damit nach dem Klick der WIRT neu zeichnet, der ihn
/// angelegt hat — nicht nur die Leiste.</para>
///
/// <para><b>Worauf sie wirkt</b>, entscheidet die Leiste nicht: Sind Kästchen gesetzt,
/// auf die gewählten Zeilen, sonst auf die Fokuszeile (Konzept 3.3). Die Leiste zählt nur
/// — liegt die Zahl außerhalb von <see cref="Mindestens"/> und <see cref="Hoechstens"/>,
/// ist der Knopf WEICH gesperrt und nennt <see cref="ZahlGrund"/>.</para>
/// </summary>
public sealed record Auswahlhandlung(string Text, EventCallback Ausfuehren)
{
    /// <summary>Die kleinste Zeilenzahl, für die die Handlung geht (Vorgabe 1).</summary>
    public int Mindestens { get; init; } = 1;

    /// <summary>Die größte Zeilenzahl, für die die Handlung geht (Vorgabe: beliebig).</summary>
    public int Hoechstens { get; init; } = int.MaxValue;

    /// <summary>Der Grund am Knopf, wenn die Zeilenzahl nicht passt („… gilt für genau eine Zeile").</summary>
    public string ZahlGrund { get; init; } = "";

    /// <summary>
    /// Ein Grund, aus dem die Handlung trotz passender Zeilenzahl nicht geht (ein
    /// Auslieferungssatz beim Löschen); leer = frei. Der Knopf ist dann WEICH gesperrt:
    /// <c>aria-disabled</c>, der Grund im Kurztext, und ein Klick meldet ihn.
    /// </summary>
    public string Sperrgrund { get; init; } = "";

    /// <summary>HART gesperrt (<c>disabled</c>) — im Lesemodus eines Wirts, der nichts schreibt.</summary>
    public bool Aus { get; init; }

    /// <summary>Ein Umschalter (<c>aria-pressed</c>) — „Vergleichen" steht gedrückt, solange verglichen wird.</summary>
    public bool? Gedrueckt { get; init; }

    /// <summary>
    /// Der Kurztext (<c>title</c>) am freien Knopf — was die Handlung tut, wo die
    /// Beschriftung es nicht ganz sagt („Schloss aufheben…": die gewählten
    /// Auslieferungssätze werden eigene Sätze). Leer = keiner. Ein weich gesperrter Knopf
    /// trägt statt dessen seinen Grund.
    /// </summary>
    public string Kurztext { get; init; } = "";

    /// <summary>
    /// <b>Eine zweite Beschriftung, deren Breite der Knopf hält</b> — die, die er im
    /// anderen Zustand trägt („Schloss setzen…" neben „Schloss aufheben…"). Wechselt die
    /// Beschriftung mit der Auswahl, bliebe sonst die Leiste nicht gleich breit, sie bräche
    /// anders um, und im schmalen Fenster spränge die Liste darunter (Katalogprobe, Fall
    /// u4). Leer = keine; die Vorlage ist unsichtbar und für die Sprachausgabe verborgen.
    /// </summary>
    public string Breitenvorlage { get; init; } = "";
}
