namespace EPOS.UI.Bausteine;

/// <summary>
/// Dringlichkeit eines <see cref="Warnbanner"/>. Bestimmt allein die Optik -
/// den Farbklang aus <c>KartenStil.cs</c> - nicht das Verhalten des Dialogs.
/// </summary>
public enum WarnStufe
{
    /// <summary>Beilaeufige Erlaeuterung. Blau wie die Hinweisflaechen der Startmaske.</summary>
    Hinweis,

    /// <summary>Etwas stimmt moeglicherweise nicht. Amber (<c>KartenStil.WARN_*</c>).</summary>
    Warnung,

    /// <summary>Der Vorgang kann so nicht fortgesetzt werden. Koralle wie die Senke.</summary>
    Fehler
}

/// <summary>
/// Zustand einer <see cref="Kohaerenzzeile"/>: Passt der genannte Wert zu dem,
/// woraus er sich ergeben muesste?
/// </summary>
public enum KohaerenzZustand
{
    /// <summary>Stimmig - der Wert deckt sich mit seiner Herleitung.</summary>
    Ok,

    /// <summary>Abweichend - der Wert und seine Herleitung passen nicht zusammen.</summary>
    Abweichend
}
