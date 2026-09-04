using EPOS.UI.Bausteine;

namespace EPOS.UI.Dialoge.Hilfe;

/// <summary>
/// <see cref="KiChatDialog"/> — die BESTAETIGUNGSSCHICHT (Fachkonzept 3.5).
/// </summary>
/// <remarks>
/// <para>
/// Eigene Teildatei, weil sie ein eigener Gegenstand ist: Der Chat zeigt eine
/// Vorschau und bleibt stehen, bis der Anwender entschieden hat — das ist die
/// Stelle, an der die Rundenschleife des Dienstes wartet, ohne einen Faden zu
/// belegen. Dasselbe Vorgehen wie bei <c>SimulationErgebnisHuelle</c>, die seit
/// iU9-W11b in vier Teildateien liegt.
/// </para>
/// <para>
/// <b>Der Block steht UNTEN im Verlauf</b> (Entscheid E-3), nicht oben am
/// Fenster wie im Vorlaeufer: Der Kommentar dort (<c>Form_KiChat.cs:609-613</c>)
/// verlangt „der Anwender soll die Vorschau neben dem lesen koennen, was zu ihr
/// gefuehrt hat" — in einer scrollenden Liste ist das unten.
/// </para>
/// </remarks>
public partial class KiChatDialog
{
    // =====================================================================
    //  Die Bestaetigungsschicht (Fachkonzept 3.5)
    // =====================================================================

    /// <summary>
    /// Zeigt eine Vorschau und wartet auf die Entscheidung des Anwenders.
    /// </summary>
    /// <remarks>
    /// <para><b>Nur EINE Vorschau gleichzeitig</b> (Bestand <c>:720</c>): Steht schon
    /// eine offen, wird die neue sofort abgelehnt — es gibt keine
    /// Sammelbestaetigung (Fachkonzept 3.5, Punkt 4).</para>
    /// <para>Der Text geht ZUSAETZLICH in den Verlauf (<c>:731-733</c>), damit er
    /// nachlesbar bleibt, wenn der Block verschwindet.</para>
    /// </remarks>
    public async Task<bool> BestaetigungZeigen(string vorschau, string verfallstext)
    {
        if (_bestaetigungOffen) return false;

        _bestaetigungText = vorschau ?? "";
        _bestaetigungVerfall = verfallstext ?? "";
        _bestaetigungOffen = true;
        _bestaetigungAntwort = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Anhaengen(new[]
        {
            new Gespraechszeile(Gespraechsrolle.Bestaetigung, Texte.BestaetigungTitel),
            new Gespraechszeile(Gespraechsrolle.Assistent, _bestaetigungText)
        });

        await InvokeAsync(StateHasChanged);
        return await _bestaetigungAntwort.Task;
    }

    /// <summary>Setzt den Verfallstext (der Wirt zaehlt herunter).</summary>
    public Task VerfallSetzen(string text)
    {
        if (!_bestaetigungOffen) return Task.CompletedTask;
        _bestaetigungVerfall = text ?? "";
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Beendet eine offene Vorschau von aussen — Verfall, Abbruch oder das
    /// Schliessen des Fensters. Mehrfachaufruf ist unschaedlich, der erste gewinnt.
    /// </summary>
    public Task BestaetigungBeenden(bool erteilt)
    {
        Entscheiden(erteilt);
        return InvokeAsync(StateHasChanged);
    }

    private void Entscheiden(bool erteilt)
    {
        TaskCompletionSource<bool>? quelle = _bestaetigungAntwort;
        if (quelle is null) return;

        _bestaetigungAntwort = null;
        _bestaetigungOffen = false;
        _bestaetigungVerfall = "";
        quelle.TrySetResult(erteilt);
    }
}
