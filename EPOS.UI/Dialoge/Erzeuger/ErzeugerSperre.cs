using WindowsFormsApplication1;
using Resource = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// Der Sperrgrund je Feld der Erzeugermasken mit Projektliste — Heizkessel, BHKW,
/// Stromspeicher, Photovoltaik, Solarkollektoren (<c>KiMaskenhaken.Sperrgrund</c>).
/// </summary>
/// <remarks>
/// <para><b>Zwei Sperren, beide vor der Bestätigung.</b> Den Energieträger setzt der
/// Assistent nie: Sein Handweg (<c>TraegerWechseln</c>) schreibt sofort in die Datenbank,
/// ein Setzen nur der Anzeigezeile ließe Datenbank, Modell und Projektzuordnung beim alten
/// Träger. Ein Feld der Projektzeile ist gesperrt, solange keine Zeile gewählt ist und die
/// Einzelwahl nicht greift (nicht genau eine Zeile).</para>
/// <para><b>Ohne bekannte Zeilenzahl keine Zeilensperre</b>: Dann bleibt die benannte Absage
/// des Setzers (<c>KI_ERZ_KEINE_PROJEKTZEILE</c>) der einzige Riegel — sie bleibt ohnehin
/// die zweite Sicherung.</para>
/// </remarks>
public static class ErzeugerSperre
{
    /// <summary>Der Feldschlüssel des Energieträgers im Dialogkatalog.</summary>
    public const string ENERGIETRAEGER = "energietraeger";

    /// <summary>
    /// Der Sperrgrund für <paramref name="feld"/>; <c>null</c> = frei.
    /// </summary>
    /// <param name="feld">Feldschlüssel des Dialogkatalogs.</param>
    /// <param name="gewaehlt">Ist eine Projektzeile gewählt?</param>
    /// <param name="zeilenzahl">Zahl der Projektzeilen; <c>null</c> = unbekannt.</param>
    /// <param name="mitTraeger">Führt die Maske den Energieträger als Feld?</param>
    /// <param name="ohneZeile">Felder, die keine gewählte Zeile brauchen (neben „Alle Daten").</param>
    public static string? Grund(string feld, bool gewaehlt, Func<int>? zeilenzahl,
                                bool mitTraeger, Func<string, bool>? ohneZeile = null)
    {
        if (string.IsNullOrWhiteSpace(feld)) return null;

        if (mitTraeger && string.Equals(feld, ENERGIETRAEGER, StringComparison.OrdinalIgnoreCase))
            return Resource.KI_ERZ_TRAEGER_VON_HAND;

        if (feld.StartsWith(KiDialoge.KATALOGFELD_VORSILBE, StringComparison.OrdinalIgnoreCase)) return null;
        if (ohneZeile?.Invoke(feld) == true) return null;
        if (gewaehlt || zeilenzahl is null) return null;

        return zeilenzahl() == 1 ? null : Resource.KI_ERZ_KEINE_PROJEKTZEILE;
    }
}
