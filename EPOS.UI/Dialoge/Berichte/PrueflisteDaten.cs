namespace EPOS.UI.Dialoge.Berichte;

/// <summary>
/// Eine Meldung des Vorlagenprüfers (BV-E1, Konzept 6.8) in der Überlagerung „Prüfliste" —
/// ohne Fachklasse des Kerns: Die Hülle formt den Befund des Prüfers in diese Zeile um.
/// </summary>
/// <param name="Stufe">
/// <see cref="Pruefstufe.Fehler"/>, <see cref="Pruefstufe.Warnung"/> oder
/// <see cref="Pruefstufe.Hinweis"/> (Groß- und Kleinschreibung gleich); eine unbekannte Stufe
/// steht bei den Hinweisen.
/// </param>
/// <param name="Text">Die Meldung („Unbekannter Platzhalter {{projekt.kundename}}").</param>
/// <param name="Fundort">Der menschliche Fundort („Tabelle 3, Zeile 2, Zelle beginnt mit ‚Wärme‘"); leer = keiner.</param>
/// <param name="WasTun">Was der Anwender tun kann („Vorschlag {{projekt.kunde}} übernehmen"); leer = nichts.</param>
/// <param name="Kennung">
/// Die <c>KiMeldungskennung</c> eines fachfremden Befunds — gesetzt, trägt die Zeile
/// „erklären lassen" (Hausregel „Eine Meldung, die der Anwender nicht versteht, bekommt eine
/// Kennung"); leer = kein Link.
/// </param>
public sealed record Pruefmeldungszeile(string Stufe, string Text, string Fundort = "",
                                        string WasTun = "", string Kennung = "");

/// <summary>
/// Die drei Stufen einer Prüfmeldung (Konzept 6.8, Tabelle „Stufe") in der Reihenfolge, in der
/// die Prüfliste sie zeigt.
/// </summary>
public static class Pruefstufe
{
    /// <summary>Die Vorlage lässt sich so nicht füllen — ein Bericht nähme den Rückfall.</summary>
    public const string Fehler = "fehler";

    /// <summary>Gefüllt wird, aber nicht so, wie es gemeint sein dürfte.</summary>
    public const string Warnung = "warnung";

    /// <summary>Eine Auskunft ohne Handlungsbedarf.</summary>
    public const string Hinweis = "hinweis";

    /// <summary>Die drei Stufen in Anzeigereihenfolge.</summary>
    public static IReadOnlyList<string> Reihenfolge { get; } = new[] { Fehler, Warnung, Hinweis };

    /// <summary>
    /// Die Stufe einer Meldung in Normalform — Groß- und Kleinschreibung gleich, eine
    /// unbekannte oder leere Stufe ist ein <see cref="Hinweis"/>.
    /// </summary>
    public static string Normalform(string? stufe)
    {
        string s = (stufe ?? "").Trim();
        if (string.Equals(s, Fehler, StringComparison.OrdinalIgnoreCase)) return Fehler;
        if (string.Equals(s, Warnung, StringComparison.OrdinalIgnoreCase)) return Warnung;
        return Hinweis;
    }
}
