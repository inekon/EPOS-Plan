using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace EPOS.UI.Tests;

/// <summary>
/// <b>Eine Zeile der Katalogliste anklicken</b> — in den Verwaltungen ist die Zeile selbst
/// die Wahl (Konzept Administrationsdialoge, Stufe 2, V4): Es gibt keinen runden
/// Wahlknopf mehr, jede Zelle trägt eine Klickfläche (<c>.epos-zeilenzelle</c>).
/// </summary>
internal static class Zeilenklick
{
    /// <summary>Die Klickflächen der ersten Zelle je Zeile, in Anzeigereihenfolge.</summary>
    internal static IReadOnlyList<IElement> Zeilen<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.FindAll(".epos-katalogliste tbody tr")
              .Select(tr => tr.QuerySelector(".epos-zeilenzelle"))
              .Where(z => z is not null)
              .Select(z => z!)
              .ToList();

    /// <summary>Klickt die Zeile <paramref name="index"/> an — auf Wunsch mit Strg.</summary>
    internal static void Zeile<T>(IRenderedComponent<T> cut, int index, bool strg = false) where T : IComponent
        => Zeilen(cut)[index].Click(new MouseEventArgs { CtrlKey = strg });

    /// <summary>Ein Tastendruck auf die Liste als Tabulatorhalt (↑ ↓ Pos1 Ende).</summary>
    internal static void Taste<T>(IRenderedComponent<T> cut, string taste) where T : IComponent
        => cut.Find(".epos-katalogliste .epos-raster-huelle[tabindex]")
              .KeyDown(new KeyboardEventArgs { Key = taste });
}
