namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// Die drei Wege, auf denen der Wirt die Bestaetigungsschicht des Chats bedient.
/// </summary>
/// <remarks>
/// <para>
/// <b>Warum die Komponente sich anmeldet statt umgekehrt.</b> Der Bestand setzte
/// <c>KiChatService.Bestaetigungsweg = _bestaetigungsweg</c> im Konstruktor des
/// Fensters (<c>Form_KiChat.cs:127</c>) - Fenster und Dienst kannten einander.
/// Eine Razor-Komponente wird von ihrer Huelle nur mit Parametern versorgt; die
/// Huelle haelt keinen Verweis auf die Instanz. Deshalb reicht die Komponente
/// ihre drei Einstiege beim Aufbau nach aussen, und die Huelle haengt sie an
/// <c>KiChatService.Bestaetigungsweg</c>.
/// </para>
/// <para>
/// Das beseitigt nebenbei den latenten Fehler des Bestands (Befund W15b-B28):
/// Zwei offene Chatfenster setzten den Weg bedingungslos, und das Schliessen des
/// zweiten liess ihn auf <c>null</c> - das erste konnte danach keine
/// Schreibaktion mehr bestaetigen. Die Huelle holt jetzt ein offenes Fenster nach
/// vorn, statt ein zweites anzulegen.
/// </para>
/// </remarks>
public sealed class KiChatSteuerung
{
    /// <summary>
    /// Zeigt eine Vorschau und wartet auf die Entscheidung: <c>true</c> = ausfuehren.
    /// Erster Wert der Vorschautext, zweiter der Verfallstext.
    /// </summary>
    public Func<string, string, Task<bool>> Zeigen { get; init; } = (_, _) => Task.FromResult(false);

    /// <summary>Setzt den Verfallstext (der Wirt zaehlt herunter).</summary>
    public Func<string, Task> Verfall { get; init; } = _ => Task.CompletedTask;

    /// <summary>
    /// Beendet eine offene Vorschau von aussen - Verfall, Abbruch oder das
    /// Schliessen des Fensters. Mehrfachaufruf ist unschaedlich.
    /// </summary>
    public Func<bool, Task> Beenden { get; init; } = _ => Task.CompletedTask;
}
