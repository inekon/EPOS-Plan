namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Die offenen Blätter einer <see cref="Ueberlagerung"/></b> (Anwenderwunsch 08.10.2026): Die Überlagerung
/// reicht ihren Stapel als <c>CascadingValue</c> an ihren Inhalt, und jeder <see cref="Blattwechsel"/> darin
/// meldet sich an, solange sein Blatt steht — mit seinem Rückweg. Schließen der Überlagerung (✕, Esc,
/// Hintergrund) führt dann nur zum vorigen Blatt zurück; erst vom Wurzelblatt aus schließt der Dialog.
///
/// <para>Kein Fachwissen, keine Oberfläche: nur die Reihenfolge der Anmeldungen. Das zuletzt geöffnete Blatt
/// liegt oben; ein Blatt in einem Blatt geht also zuerst zurück.</para>
/// </summary>
public sealed class Blattstapel
{
    private readonly List<(object Blatt, Func<Task> Rueckweg)> _offen = new();

    /// <summary>Steht in der Überlagerung mindestens ein Blatt?</summary>
    public bool BlattOffen => _offen.Count > 0;

    /// <summary>Die Zahl der stehenden Blätter (Prüfhilfe).</summary>
    public int Anzahl => _offen.Count;

    /// <summary>Ein Blatt steht — angemeldet wird es einmal, ein zweiter Aufruf erneuert nur den Rückweg.</summary>
    public void Anmelden(object blatt, Func<Task> rueckweg)
    {
        int i = _offen.FindIndex(e => ReferenceEquals(e.Blatt, blatt));
        if (i >= 0) _offen[i] = (blatt, rueckweg);
        else _offen.Add((blatt, rueckweg));
    }

    /// <summary>Ein Blatt ist zu oder abgebaut; ein nicht angemeldetes bleibt folgenlos.</summary>
    public void Abmelden(object blatt) => _offen.RemoveAll(e => ReferenceEquals(e.Blatt, blatt));

    /// <summary>
    /// Führt das oberste Blatt zurück (sein Rückweg, wie Rückknopf und Esc des Blattes); <c>false</c> = es
    /// stand keines, der Aufrufer schließt selbst. Abgemeldet wird das Blatt erst, wenn der Wirt es schließt —
    /// bleibt es stehen, fragt das nächste Schließen es erneut.
    /// </summary>
    public async Task<bool> ZurueckZumVorigen()
    {
        if (_offen.Count == 0) return false;
        await _offen[^1].Rueckweg();
        return true;
    }
}
