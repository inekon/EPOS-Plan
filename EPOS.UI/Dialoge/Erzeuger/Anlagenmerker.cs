namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Der Stand der anlagenbezogenen Felder einer Projektzeile beim Öffnen der
/// Satz-Überlagerung (ÜS1): Träger, Vorlauf, Rücklauf samt Herkunftszeile und
/// Grenzleistung. Diese Felder gehen schon bei der Eingabe in die Arbeitskopie der
/// Anlage (der Träger sogar sofort in die Datenbank); „Abbrechen" der Überlagerung
/// stellt mit <see cref="Zurueckschreiben"/> den Stand beim Öffnen wieder her.
/// </summary>
public readonly record struct Anlagenmerker(int CarrierId, int? Vorlauf, int? Ruecklauf,
                                            string TemperaturHerleitung, double? Grenzleistung)
{
    /// <summary>Der Stand der Zeile jetzt.</summary>
    public static Anlagenmerker Von(ErzeugerZeile zeile)
        => new(zeile.CarrierId, zeile.Vorlauf, zeile.Ruecklauf, zeile.TemperaturHerleitung, zeile.Grenzleistung);

    /// <summary>
    /// Schreibt den gemerkten Stand in die Zeile zurück — nur, was sich geändert hat: ein
    /// anderer Träger geht über <paramref name="traegerWechseln"/> (derselbe Weg wie die
    /// Trägerwahl), die Zahlenfelder über <paramref name="uebernehmen"/> ins Modell.
    /// </summary>
    /// <returns><c>true</c>, wenn etwas zurückgeschrieben wurde.</returns>
    public bool Zurueckschreiben(ErzeugerZeile zeile, Action<ErzeugerZeile, int>? traegerWechseln,
                                 Action<ErzeugerZeile>? uebernehmen)
    {
        bool geaendert = false;
        if (zeile.CarrierId != CarrierId && CarrierId > 0)
        {
            traegerWechseln?.Invoke(zeile, CarrierId);
            zeile.CarrierId = CarrierId;
            geaendert = true;
        }
        if (zeile.Vorlauf != Vorlauf || zeile.Ruecklauf != Ruecklauf || zeile.Grenzleistung != Grenzleistung
            || zeile.TemperaturHerleitung != TemperaturHerleitung)
        {
            zeile.Vorlauf = Vorlauf;
            zeile.Ruecklauf = Ruecklauf;
            zeile.Grenzleistung = Grenzleistung;
            zeile.TemperaturHerleitung = TemperaturHerleitung;
            uebernehmen?.Invoke(zeile);
            geaendert = true;
        }
        return geaendert;
    }
}
