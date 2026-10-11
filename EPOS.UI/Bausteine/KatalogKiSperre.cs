using System.Globalization;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Was die Fangstelle meldet, sagt auch der Assistent</b> (KT-8b). Seit KT-7 laufen die
/// Datenbankwege der Katalogdialoge über <see cref="Katalogladung"/>: Wirft das Lesen der Liste
/// oder des gewählten Satzes, bleibt der Dialog offen und ein Warnbanner nennt den Grund. Dieser
/// Baustein übersetzt dieselben zwei Texte für die KI-Sicht — als lesbares Feld
/// (<see cref="Ladestand"/>), als Sperrgrund je Feld (<see cref="Feldsperre"/>, für den Haken
/// <c>KiMaskenhaken.Sperrgrund</c>) und als Absage einer Aktion (<see cref="Aktionssperre"/>,
/// vor Speichern, Übernehmen, Anlegen).
/// </summary>
/// <remarks>
/// <para><b>Die Liste sperrt alles</b>: Ohne Liste kennt der Dialog weder Sätze noch Klapplisten
/// (<c>KI_DLG_LISTE_NICHT_GELADEN</c>).</para>
/// <para><b>Der Satz sperrt alles außer der Satzwahl</b>: Ein halb gelesener Satz wird nicht
/// geändert und nicht gespeichert, ein anderer Satz lässt sich wählen
/// (<c>KI_DLG_SATZ_NICHT_GELADEN</c>).</para>
/// <para>Der Baustein hält keinen Zustand — der Wirt reicht seine beiden Bannertexte bei jedem
/// Aufruf herein.</para>
/// </remarks>
public static class KatalogKiSperre
{
    /// <summary>Die Bannertexte, die stehen, verbunden; leer = alles geladen.</summary>
    public static string Ladestand(params string?[] fehler)
        => string.Join(" ", fehler.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f!.Trim()));

    /// <summary>
    /// Der Sperrgrund eines Feldes; leer = frei. <paramref name="satzwahl"/> ist der Schlüssel
    /// des Wahlfeldes, das trotz eines gescheiterten Satzes frei bleibt (<c>null</c> = keines).
    /// </summary>
    public static string Feldsperre(string feld, string? listeFehler, string? satzFehler, string? satzwahl)
    {
        if (!string.IsNullOrWhiteSpace(listeFehler)) return Liste(listeFehler!);
        if (!string.IsNullOrWhiteSpace(satzFehler) && !string.Equals(feld, satzwahl, StringComparison.Ordinal))
            return Satz(satzFehler!);
        return "";
    }

    /// <summary>Der Grund, eine Aktion (Speichern, Übernehmen, Anlegen) abzulehnen; <c>null</c> = frei.</summary>
    public static string? Aktionssperre(string? listeFehler, string? satzFehler)
    {
        if (!string.IsNullOrWhiteSpace(listeFehler)) return Liste(listeFehler!);
        if (!string.IsNullOrWhiteSpace(satzFehler)) return Satz(satzFehler!);
        return null;
    }

    private static string Liste(string fehler)
        => string.Format(CultureInfo.CurrentCulture, Resource.KI_DLG_LISTE_NICHT_GELADEN, fehler.Trim());

    private static string Satz(string fehler)
        => string.Format(CultureInfo.CurrentCulture, Resource.KI_DLG_SATZ_NICHT_GELADEN, fehler.Trim());
}
