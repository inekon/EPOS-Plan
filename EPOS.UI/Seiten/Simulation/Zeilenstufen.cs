namespace EPOS.UI.Seiten.Simulation;

/// <summary>
/// Die Stufe einer Wertzeile in der Kennzahlenliste eines Erzeugerreiters: eine gewöhnliche
/// Zeile, eine eingerückte Zeile des Herleitungsblocks oder dessen Ergebnis unter dem
/// Summenstrich.
/// </summary>
public enum Zeilenstufe
{
    Normal,
    Herleitung,
    Ergebnis,
}

/// <summary>Die CSS-Klassen der Wertzeilen je <see cref="Zeilenstufe"/>.</summary>
public static class Zeilenstufen
{
    /// <summary>
    /// Die Klasse für Beschriftung und Wert. <c>null</c> lässt Blazor das Attribut ganz weg —
    /// kein leeres class="". Eingerückt wird nur die Beschriftung.
    /// </summary>
    public static string? Klasse(bool betont, Zeilenstufe stufe, bool beschriftung = false)
    {
        var teile = new List<string>(2);
        if (betont) teile.Add("epos-simerg-abschluss");
        if (stufe == Zeilenstufe.Herleitung && beschriftung) teile.Add("epos-simerg-eingerueckt");
        if (stufe == Zeilenstufe.Ergebnis) teile.Add("epos-simerg-zwischensumme");
        return teile.Count == 0 ? null : string.Join(' ', teile);
    }

    /// <summary>Die Klasse der Einheitsspalte.</summary>
    public static string Einheitsklasse(bool betont, Zeilenstufe stufe)
    {
        string? weitere = Klasse(betont, stufe);
        return weitere is null ? "epos-simerg-einheit" : "epos-simerg-einheit " + weitere;
    }
}
