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
    Fehler,

    /// <summary>
    /// Es hat GEKLAPPT — gruen, mit Haken (iU9-W10a.4).
    ///
    /// <para>Der WinForms-Bestand meldet einen Erfolg nicht mit einer MessageBox,
    /// sondern mit einer gruenen Statuszeile: <c>„✔ Pufferspeicher angelegt."</c>
    /// (<c>Form_PufferSp_Projekt.Status</c>, <c>Color.ForestGreen</c>) und ebenso in
    /// der Simulationskonfiguration (<c>ShowStatus</c>, drei Sekunden sichtbar). Ohne
    /// diese Stufe waere daraus eine Warnung geworden — dieselbe Optik fuer „hat
    /// geklappt" und „stimmt etwas nicht".</para>
    ///
    /// <para>Der Drei-Sekunden-Timer des Vorlaeufers kommt NICHT mit: Ein Banner, das
    /// von selbst verschwindet, ist fuer eine Sprachausgabe schwer erreichbar. Es
    /// bleibt stehen, bis die naechste Handlung es ersetzt.</para>
    /// </summary>
    Erfolg
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
