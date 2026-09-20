namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Was die x-Achse eines Diagramms ZÄHLT</b> — die Tabelle „Was x je Bild
/// bedeutet" des Protokolls DG-E3.
///
/// <para>Das Zeichenmodell nennt die Einheit seiner x-Achse nicht: Es trägt
/// Datenfenster, und ein Datenfenster kennt nur Zahlen. Der Baustein
/// <see cref="DiagrammSvg"/> braucht die Angabe an zwei Stellen — in der
/// Zeigerzeile („4.000 h" gegen „4.000") und beim Nachzeichnen der Achsenteilung
/// im Ausschnitt (die Jahresstundenteilung des Kerns gegen eine ganzzahlige
/// Teilung).</para>
/// </summary>
public enum Achsenart
{
    /// <summary>
    /// Die JAHRESSTUNDE 0 … n−1 (Jahresgang, Jahresverlauf). Nur sie nimmt
    /// <c>ChartRenderer.Jahresstundenteilung</c>, und nur sie trägt die Einheit „h".
    /// </summary>
    Jahresstunde = 0,

    /// <summary>
    /// Der INDEX der Reihe (Kostenprofil, Stundenprofil). Das Bild legt das Profil
    /// über seine eigene Länge; ein Profil muss keine 8 760 Werte führen, und die
    /// Achse zählt deshalb keine Stunde des Jahres.
    /// </summary>
    Index = 1,

    /// <summary>
    /// Die STÜTZSTELLE 0 … n−1 (GanglinieNormiert, ErzeugerStapel,
    /// Temperaturverlauf, Speicherbetrieb) — Stunden oder Viertelstunden, je nach
    /// Reihenlänge.
    /// </summary>
    Stuetzstelle = 2,

    /// <summary>
    /// Der RANG in der Dauerlinie: dieselben Werte, absteigend sortiert. x zählt
    /// dann nicht mehr die Zeit, sondern den Platz in der Rangfolge.
    /// </summary>
    Rang = 3
}
