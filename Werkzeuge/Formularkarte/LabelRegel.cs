namespace Formularkarte;

/// <summary>
/// Ordnet jedem Eingabefeld seine Beschriftung zu.
///
/// <para>
/// Das im Konzept vermutete Raster "Label bei x=28, Steuerelement bei x=270"
/// gibt es im Bestand nicht: Ueber alle Designer unter <c>Views/</c> liegen die
/// Label-x-Werte bei 12, 13, 14, 15, 16, 18 und die Feld-x-Werte je Maske ganz
/// verschieden (114, 159, 250, 278, 350 ...). Tragfaehig ist stattdessen die
/// Zeilenregel:
/// </para>
/// <list type="number">
///   <item>das naechste Label LINKS in derselben Zeile
///         (|dy| &lt;= 8 px, kleineres x, gleicher Abschnitt),</item>
///   <item>sonst das Label direkt DARUEBER (dy &lt;= 24 px, gleiches x +- 8 px),</item>
///   <item>sonst keine Beschriftung.</item>
/// </list>
/// <para>
/// Ein Label wird nur einmal vergeben. Knoepfe bekommen keine Beschriftung -
/// ihre Aufschrift steht in <c>Text</c>.
/// </para>
/// </summary>
public static class LabelRegel
{
    /// <summary>Hoechster senkrechter Versatz, der noch als "dieselbe Zeile" gilt.</summary>
    public const int ZeileToleranz = 8;

    /// <summary>Hoechster Abstand nach oben, der noch als "direkt darueber" gilt.</summary>
    public const int ObenAbstand = 24;

    /// <summary>Hoechster seitlicher Versatz beim Label darueber.</summary>
    public const int ObenToleranz = 8;

    /// <summary>Vergibt die Beschriftungen der ganzen Maske.</summary>
    public static void Anwenden(Maske maske)
    {
        var labels = maske.Steuerelemente
            .Where(s => s.Art == Art.Beschriftung && s.Ort is not null)
            .ToList();

        var felder = maske.Steuerelemente
            .Where(BrauchtBeschriftung)
            .OrderBy(s => s.Y).ThenBy(s => s.X).ThenBy(s => s.Reihenfolge)
            .ToList();

        foreach (var feld in felder)
        {
            feld.Beschriftung = Suchen(feld, labels);
            if (feld.Beschriftung is not null) feld.Beschriftung.AlsBeschriftungVerbraucht = true;
        }
    }

    /// <summary>Bekommt dieses Steuerelement eine Beschriftung aus einem Label?</summary>
    public static bool BrauchtBeschriftung(Steuerelement element) =>
        element.Ort is not null && element.Art is Art.Feld or Art.Sonstig;

    private static Steuerelement? Suchen(Steuerelement feld, List<Steuerelement> labels)
    {
        // 1. Links in derselben Zeile - das naechstliegende gewinnt.
        Steuerelement? links = null;
        foreach (var label in labels)
        {
            if (label.AlsBeschriftungVerbraucht) continue;
            if (!GleicherAbschnitt(label, feld)) continue;
            if (label.X >= feld.X) continue;
            if (Math.Abs(label.Y - feld.Y) > ZeileToleranz) continue;
            if (links is null || label.X > links.X) links = label;
        }
        if (links is not null) return links;

        // 2. Direkt darueber - das tiefstliegende gewinnt.
        Steuerelement? oben = null;
        foreach (var label in labels)
        {
            if (label.AlsBeschriftungVerbraucht) continue;
            if (!GleicherAbschnitt(label, feld)) continue;
            if (label.Y >= feld.Y) continue;
            if (feld.Y - label.Y > ObenAbstand) continue;
            if (Math.Abs(label.X - feld.X) > ObenToleranz) continue;
            if (oben is null || label.Y > oben.Y) oben = label;
        }
        return oben;
    }

    private static bool GleicherAbschnitt(Steuerelement a, Steuerelement b) =>
        string.Equals(a.Elter, b.Elter, StringComparison.Ordinal);
}
