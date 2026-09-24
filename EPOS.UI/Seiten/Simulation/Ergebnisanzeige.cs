namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// EIN Anzeigeschalter eines Ergebnisblattes für den Hilfe-Assistenten (Welle #458,
/// Stufe 2) — „Jahresdauerlinie", eine Reihe ein oder aus.
/// </summary>
/// <remarks>
/// <b>Er schreibt nichts:</b> Ein Anzeigeschalter stellt das Bild ein, nicht den Stand.
/// Gesetzt wird über denselben Weg wie der Schalter auf dem Blatt; das Blatt zeichnet
/// sich danach selbst neu.
/// </remarks>
public sealed class Anzeigeschalter
{
    private readonly Func<bool> _lesen;
    private readonly Action<bool> _setzen;

    /// <summary>Legt einen Schalter über die Wege des Blattes.</summary>
    /// <param name="name">Die Beschriftung des Schalters — sein Zeilenkennzeichen.</param>
    public Anzeigeschalter(string name, Func<bool> lesen, Action<bool> setzen)
    {
        Name = name ?? "";
        _lesen = lesen ?? throw new ArgumentNullException(nameof(lesen));
        _setzen = setzen ?? throw new ArgumentNullException(nameof(setzen));
    }

    /// <summary>Die Beschriftung des Schalters, wie sie auf dem Blatt steht.</summary>
    public string Name { get; }

    /// <summary>Steht der Schalter an?</summary>
    public bool An
    {
        get => _lesen();
        set => _setzen(value);
    }
}

/// <summary>
/// Das Register der Anzeigeschalter der GEZEICHNETEN Ergebnisblätter (Welle #458,
/// Stufe 2) — die Seite reicht es als Kaskadenwert an ihre Blätter, jedes meldet beim
/// Aufbau seine Schalter an und beim Abbau ab.
/// </summary>
/// <remarks>
/// <para><b>Warum ein Register und keine Verweise auf die Blätter.</b> Ein Reiter zeichnet
/// nur sein sichtbares Blatt; die übrigen gibt es gar nicht. Das Register führt deshalb
/// genau die Schalter, die gerade vor dem Anwender stehen — auch die des inneren Blattes
/// „Wärme-/Stromproduktion" im Blatt „Ergebnis" —, ohne dass die Seite die neun Blätter
/// kennen müsste.</para>
/// <para><b>Die Reihenfolge ist die der Anmeldung</b>, innerhalb eines Blattes die seiner
/// Schalter von oben nach unten.</para>
/// </remarks>
public sealed class Ergebnisanzeige
{
    private readonly List<(object Halter, Func<IReadOnlyList<Anzeigeschalter>> Schalter)> _blaetter = new();

    /// <summary>Meldet die Schalter eines Blattes an; eine zweite Anmeldung ersetzt die erste.</summary>
    public void Melden(object halter, Func<IReadOnlyList<Anzeigeschalter>> schalter)
    {
        Abmelden(halter);
        _blaetter.Add((halter, schalter));
    }

    /// <summary>Meldet ein Blatt ab; mehrfaches Abmelden ist erlaubt.</summary>
    public void Abmelden(object halter) => _blaetter.RemoveAll(b => ReferenceEquals(b.Halter, halter));

    /// <summary>Die Schalter aller gezeichneten Blätter — bei jedem Zugriff neu gefragt.</summary>
    public IReadOnlyList<Anzeigeschalter> Schalter
    {
        get
        {
            var liste = new List<Anzeigeschalter>();
            foreach (var blatt in _blaetter.ToList()) liste.AddRange(blatt.Schalter());
            return liste;
        }
    }
}
