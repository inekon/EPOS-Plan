namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Der Stand der anlagenbezogenen Felder einer Photovoltaik-Projektzeile beim Öffnen der
/// Satz-Überlagerung (ÜS1): Energieträger, Neigung, Azimut, Modulanzahl und das Ertragsmodell
/// (Modellweg, Wechselrichter-Wirkungsgrad, Systemverluste, Albedo). Diese Felder gehen schon bei
/// der Eingabe in die Arbeitskopie der Anlage (der Träger sogar sofort in die Datenbank);
/// „Abbrechen" der Überlagerung stellt mit <see cref="Zurueckschreiben"/> den Stand beim Öffnen her.
/// </summary>
public readonly record struct PvAnlagenmerker(int CarrierId, int? Neigung, int? Azimut, double? AnzahlModule,
                                              bool ModellErweitert, double? WrWirkungsgrad, double? Systemverluste,
                                              double? Albedo)
{
    /// <summary>Der Stand der Zeile jetzt.</summary>
    public static PvAnlagenmerker Von(ErzeugerZeile zeile)
        => new(zeile.CarrierId, zeile.Neigung, zeile.Azimut, zeile.AnzahlModule, zeile.ModellErweitert,
               zeile.WrWirkungsgrad, zeile.Systemverluste, zeile.Albedo);

    /// <summary>
    /// Schreibt den gemerkten Stand in die Zeile zurück — nur, was sich geändert hat: ein anderer
    /// Träger über <paramref name="traegerWechseln"/> (derselbe Weg wie die Trägerwahl), die übrigen
    /// Felder über <paramref name="uebernehmen"/> ins Modell.
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
        if (this != Von(zeile) with { CarrierId = CarrierId })
        {
            zeile.Neigung = Neigung;
            zeile.Azimut = Azimut;
            zeile.AnzahlModule = AnzahlModule;
            zeile.ModellErweitert = ModellErweitert;
            zeile.WrWirkungsgrad = WrWirkungsgrad;
            zeile.Systemverluste = Systemverluste;
            zeile.Albedo = Albedo;
            uebernehmen?.Invoke(zeile);
            geaendert = true;
        }
        return geaendert;
    }
}
