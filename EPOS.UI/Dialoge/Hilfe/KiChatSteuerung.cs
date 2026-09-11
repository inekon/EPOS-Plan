using KiKern;

namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// Die Kontextangabe eines Chatfensters (Auftrag #199, Stufe S1): Bereich,
/// Dialogname, Meldungskennung und die vorbelegte Frage.
/// </summary>
/// <remarks>
/// <b>Wofür ein eigener Satz und nicht vier Parameter.</b> Beim ERSTEN Öffnen kommen
/// die vier Angaben als Parameter herein; steht das Fenster aber schon (Windows
/// öffnet nur EINES, <c>KiChatHuelle</c>), müssen sie NACHTRÄGLICH gesetzt werden —
/// wer aus einem zweiten Dialog fragt, bekommt sonst den Bereich von vorhin. Der Weg
/// dafür ist derselbe wie bei der Bestätigungsschicht: Die Komponente meldet sich an,
/// der Wirt ruft.
/// </remarks>
/// <param name="Kontext">Der Bereich, fertig formuliert („Bereich: Heizkessel").</param>
/// <param name="Dialogname">Der Name des rufenden Dialogs; leer = keiner.</param>
/// <param name="Kennung">Die Meldungskennung; leer = keine.</param>
/// <param name="Vorbelegung">Die vorbelegte Frage; leer = keine.</param>
/// <param name="Hilfeschluessel">
/// Der Hilfeschlüssel des Aufrufs (Auftrag #227); leer = der Menüweg. Die
/// STARTZEILE des Chats hängt daran — nicht am Bereich, der auch der Menüweg
/// mitbringt.
/// </param>
public sealed record KiKontextangabe(string Kontext, string Dialogname,
                                     string Kennung, string Vorbelegung,
                                     string Hilfeschluessel = "");

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

    /// <summary>
    /// Stellt das STEHENDE Chatfenster auf einen neuen Aufrufkontext ein
    /// (Auftrag #199): Bereich, Dialogname, Meldungskennung und vorbelegte Frage.
    /// Der Gesprächsverlauf bleibt — er gehört der Sitzung, nicht dem Dialog.
    /// </summary>
    public Func<KiKontextangabe, Task> Kontext { get; init; } = _ => Task.CompletedTask;

    // =====================================================================
    //  Der laufende Rechenvorgang (Auftrag #214)
    // =====================================================================

    /// <summary>
    /// Die SENKE für die Fortschrittsschritte lang laufender Aktionen. Die Hülle legt
    /// sie an <c>KiAusfuehrung.Fortschritt</c>, solange das Chatfenster steht, und nimmt
    /// sie beim Schließen wieder heraus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sie gehört dem DIALOG, nicht der Hülle</b> (Auftrag #214, Punkt 1). Bis dahin
    /// belegte niemand die Senke: Der Kern meldete brav seine Phasen, und es hörte
    /// keiner zu. Läge der Fortschrittszustand in der Windows-Hülle, müsste ihn die
    /// iOS-Hülle ein zweites Mal bauen — der Baustein <c>Fortschritt</c> steht aber
    /// ohnehin in dieser Bibliothek.
    /// </para>
    /// <para>
    /// <b>Sie marshallt selbst.</b> Die Komponente nimmt jeden Schritt über
    /// <c>InvokeAsync</c> entgegen; der Kern darf also aus jedem Faden melden — und seit
    /// Auftrag #214 tut er das aus einem Arbeitsfaden.
    /// </para>
    /// </remarks>
    public IProgress<KiFortschritt> Fortschritt { get; init; } = new NichtsHoert();

    /// <summary>
    /// Die Abbruchmarke der GERADE laufenden Anforderung; ohne laufende Anforderung
    /// <see cref="CancellationToken.None"/>.
    /// </summary>
    /// <remarks>
    /// <b>Ein Delegat und kein Wert</b>, aus demselben Grund wie bei den Feldwerten
    /// (Auftrag #200): Die Quelle entsteht erst mit dem Klick und wird mit jedem neuen
    /// Lauf ersetzt. Die Hülle fragt sie unmittelbar vor dem Aufruf — dann gilt die
    /// Marke des Laufs, den sie gerade startet.
    /// </remarks>
    public Func<CancellationToken> Abbruchmarke { get; init; } = () => CancellationToken.None;

    /// <summary>Die Vorgabesenke: Sie nimmt jeden Schritt entgegen und tut nichts.</summary>
    private sealed class NichtsHoert : IProgress<KiFortschritt>
    {
        /// <inheritdoc/>
        public void Report(KiFortschritt wert) { }
    }
}
