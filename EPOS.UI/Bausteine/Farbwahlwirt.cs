using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Der Wirt eines oder mehrerer <see cref="DiagrammSvg"/> — mit der Farbwahl am
/// Bild</b> (Farbrollen, Bedienung Teil 2; Entscheid DG-E2-1).
///
/// <para><b>Warum als Rumpf und nicht je Komponente.</b> Ein Klick auf das Farbfeld
/// eines Legendeneintrags läuft überall denselben Weg: Die Komponente reicht Rolle und
/// Farbe an ihre Hülle, die Hülle schreibt sie über <c>Diagrammfarben</c>
/// anwendungsweit, und danach zeichnet sich die Komponente mit
/// <c>Farbpalette.Aktuell</c> neu. Das sind zwei Parameter und zwei Rückrufe — in
/// zwanzig Reitern und Dialogen dieselben zwanzig Zeilen. Sie stehen deshalb EINMAL
/// hier; ein Wirt schreibt <c>@inherits Farbwahlwirt</c> und hat sie.</para>
///
/// <para><b>Die Modelle bleiben dabei, wie sie sind:</b> Sie tragen Farbrollen, keine
/// Zahlen. Gezeichnet wird gegen die Palette, und die hat die Hülle inzwischen
/// gesetzt — deshalb genügt ein Zeichenlauf, und weder Zoom noch abgewählte Reihe
/// gehen verloren.</para>
/// </summary>
public abstract class Farbwahlwirt : ComponentBase
{
    /// <summary>
    /// Die Farbe einer Rolle anwendungsweit setzen (<c>Diagrammfarben.Setze</c> in der
    /// Hülle). <b>Ohne Delegat bietet kein Diagramm den Wähler an</b> — ein Wähler
    /// ohne Empfänger wäre ein Versprechen, das niemand einlöst.
    /// </summary>
    [Parameter] public Func<Farbrolle, Farbe, Task>? FarbeSetzen { get; set; }

    /// <summary>„Hausfarbe": die Rolle wieder auf die Vorgabe.</summary>
    [Parameter] public Func<Farbrolle, Task>? FarbeZuruecksetzen { get; set; }

    /// <summary>Bietet dieser Wirt den Farbwähler an? Kein Delegat, kein Wähler.</summary>
    protected bool Farbwahl => FarbeSetzen is not null;

    /// <summary>Der Anwender hat am Bild eine Farbe gewählt.</summary>
    protected async Task FarbeUebernehmen((Farbrolle Rolle, Farbe Farbe) wahl)
    {
        if (FarbeSetzen is null) return;
        await FarbeSetzen(wahl.Rolle, wahl.Farbe);
        StateHasChanged();
    }

    /// <summary>„Hausfarbe" — dieselbe Runde, nur ohne Farbe.</summary>
    protected async Task FarbeVerwerfen(Farbrolle rolle)
    {
        if (FarbeZuruecksetzen is null) return;
        await FarbeZuruecksetzen(rolle);
        StateHasChanged();
    }
}
