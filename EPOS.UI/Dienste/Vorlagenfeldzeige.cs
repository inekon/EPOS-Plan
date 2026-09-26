using EPOS.UI.Seiten;

namespace EPOS.UI.Dienste;

/// <summary>Warum „In der App zeigen" für einen Platzhalter nicht geht — oder dass es geht.</summary>
public enum Zeigegrund
{
    /// <summary>Ein lesend erreichbarer Ort steht bereit.</summary>
    Moeglich,

    /// <summary>Kein Gegenstück in der App — der Platzhalter lebt nur im Bericht.</summary>
    KeinOrt,

    /// <summary>Nur in einer Eingabemaske zu sehen — dorthin führt der Katalog nicht.</summary>
    NichtLesend,

    /// <summary>Nur im Projektassistenten (Bearbeitungsmodus) — dorthin führt der Katalog nie.</summary>
    Assistent,
}

/// <summary>
/// <b>„In der App zeigen"</b> (Konzept Berichtsvorlagen 9.4, 9.3: der Katalog verbindet D mit C) —
/// die Regel, welcher Ort in Frage kommt, und der Sprung dorthin: Stellung „Marken" einschalten,
/// die Marke aufleuchten lassen, dann über <c>Dienste.Navigation</c> zur Ansicht — derselbe Weg,
/// den der Hilfe-Assistent nimmt (<see cref="KiAssistentWeg"/>); die Schale entscheidet, was der
/// Maskenschlüssel bedeutet.
/// </summary>
public static class Vorlagenfeldzeige
{
    /// <summary>Die Ansichten des Projektassistenten — Bearbeitungsmodus, nie ein Ziel.</summary>
    private static readonly HashSet<string> ASSISTENT = new(StringComparer.Ordinal)
    {
        Seitenschluessel.Assistent, Seitenschluessel.ProjektNeu, Seitenschluessel.ProjektBearbeiten,
    };

    /// <summary>
    /// Der erste Ort, der lesend erreichbar und kein Assistent ist, und der Grund, wenn es keinen
    /// gibt (der schwerste der vorhandenen: Assistent vor „nicht lesend" vor „kein Ort").
    /// </summary>
    public static (Vorlagenfeldort? Ort, Zeigegrund Grund) Waehle(IReadOnlyList<Vorlagenfeldort>? orte)
    {
        if (orte is null || orte.Count == 0) return (null, Zeigegrund.KeinOrt);

        Zeigegrund grund = Zeigegrund.KeinOrt;
        foreach (Vorlagenfeldort o in orte)
        {
            if (o is null || string.IsNullOrWhiteSpace(o.Ansicht)) continue;
            if (ASSISTENT.Contains(o.Ansicht)) { grund = Zeigegrund.Assistent; continue; }
            if (!o.NurLesend) { if (grund == Zeigegrund.KeinOrt) grund = Zeigegrund.NichtLesend; continue; }
            return (o, Zeigegrund.Moeglich);
        }
        return (null, grund);
    }

    /// <summary>
    /// Springt zum Ort: „Marken" an, Marke <paramref name="schluessel"/> leuchtet, dann die Ansicht
    /// über <c>Dienste.Navigation</c> (mit <see cref="Vorlagenfeldort.Reiter"/> als erstem Argument,
    /// wenn gesetzt).
    /// </summary>
    /// <returns><c>false</c>, wenn die Oberfläche die Ansicht nicht kennt — dann bleibt die Anzeige,
    /// wie sie war.</returns>
    public static bool Zeigen(Vorlagenfeldansicht ansicht, Vorlagenfeldort ort, string schluessel)
    {
        if (ansicht is null || ort is null || string.IsNullOrWhiteSpace(ort.Ansicht)) return false;
        if (ASSISTENT.Contains(ort.Ansicht) || !ort.NurLesend) return false;

        Vorlagenfeldstellung vorher = ansicht.Stellung;
        ansicht.Leuchten(schluessel);

        bool gezeigt;
        try
        {
            object[] argumente = string.IsNullOrWhiteSpace(ort.Reiter) ? Array.Empty<object>() : new object[] { ort.Reiter };
            gezeigt = WindowsFormsApplication1.Dienste.Navigation.OeffneMaske(ort.Ansicht, argumente);
        }
        catch (Exception) { gezeigt = false; }

        if (!gezeigt)
        {
            ansicht.LeuchtenBeenden(schluessel);
            ansicht.Setzen(vorher);
        }
        return gezeigt;
    }
}
