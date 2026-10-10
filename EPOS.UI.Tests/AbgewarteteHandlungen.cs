using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Handlungen des Prüfstands, die abgewartet werden</b> (Befund WT-c).
/// </summary>
/// <remarks>
/// <para>bunits synchrone Auslöser (<c>Click()</c>, <c>Change()</c>, <c>KeyDown()</c>) geben die Handlung an den Verteiler der
/// Komponente. Ist er frei, läuft sie sofort samt Zeichenlauf; arbeitet er noch — nach einem Lesegang im Arbeitsfaden etwa an
/// dessen Nachläufen, an <c>OnAfterRenderAsync</c> oder an einem <c>InvokeAsync(StateHasChanged)</c> —, wird sie nur
/// eingereiht, und der Auslöser kehrt zurück, bevor sie gewirkt hat. Ein Sofort-Assert liest dann den Stand vor der Handlung;
/// unter Last ist das Fenster breit genug, dass Fälle sporadisch rot werden.</para>
/// <para>Diese Auslöser warten die Aufgabe der Handlung ab (bunits <c>…Async</c>-Form), höchstens <see cref="FRIST"/>.
/// Ein Fehler des Handlers kommt dabei wie beim synchronen Auslöser beim Aufrufer an.</para>
/// </remarks>
internal static class AbgewarteteHandlungen
{
    /// <summary>Die Obergrenze des Wartens — keine Wartezeit, nur der Schutz gegen ein Hängen.</summary>
    private static readonly TimeSpan FRIST = TimeSpan.FromSeconds(30);

    /// <summary><c>@onclick</c>, abgewartet.</summary>
    public static void KlickAbgewartet(this IElement element)
        => Abwarten(element.ClickAsync(new MouseEventArgs()));

    /// <summary><c>@onchange</c> mit <paramref name="wert"/>, abgewartet.</summary>
    public static void WechselAbgewartet<T>(this IElement element, T wert)
        => Abwarten(element.ChangeAsync(wert));

    /// <summary><c>@onkeydown</c>, abgewartet.</summary>
    public static void TasteAbgewartet(this IElement element, KeyboardEventArgs taste)
        => Abwarten(element.KeyDownAsync(taste));

    /// <summary>Wartet eine Arbeit am Verteiler ab (etwa <c>cut.InvokeAsync(…)</c>).</summary>
    public static void Abwarten(Task aufgabe) => aufgabe.WaitAsync(FRIST).GetAwaiter().GetResult();
}
