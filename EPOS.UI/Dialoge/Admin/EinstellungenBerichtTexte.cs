using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Admin;

/// <summary>
/// BV-E1 (Konzept Berichtsvorlagen 10.3) — die Beschriftungen des Abschnitts „Bericht" der
/// Programmeinstellungen (<see cref="EinstellungenDialog"/>): Firma für
/// <c>{{ersteller.firma}}</c> und der Vorlagenordner der eigenen Berichtsvorlagen.
/// </summary>
/// <remarks>
/// Ein BÜNDEL (Hausregel „ab etwa zehn Anzeigetexten eines"), Beschriftungen, kein Zustand.
/// Jede Eigenschaft füllt sich selbst aus <c>MyResource</c> und nennt ihren Schlüssel
/// (<c>EIN_BERICHT_*</c>); solange ein Schlüssel fehlt, gilt der deutsche Rückfall.
/// </remarks>
public sealed class EinstellungenBerichtTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>EIN_BERICHT_RUBRIK — der Reiter der Rubrikenliste.</summary>
    public string Rubrik { get; set; } = T("EIN_BERICHT_RUBRIK", "Bericht");

    /// <summary>EIN_BERICHT_HINWEIS — die Zeile über den Feldern, neben dem Hilfeknopf.</summary>
    public string Hinweis { get; set; } = T("EIN_BERICHT_HINWEIS",
        "Angaben für Berichte aus Word-Vorlagen – für alle Projekte dieses Anwenders.");

    /// <summary>EIN_BERICHT_LBL_FIRMA</summary>
    public string LabelFirma { get; set; } = T("EIN_BERICHT_LBL_FIRMA", "Firma:");

    /// <summary>EIN_BERICHT_HINT_FIRMA — die Herleitung unter dem Feld.</summary>
    public string HinweisFirma { get; set; } = T("EIN_BERICHT_HINT_FIRMA",
        "Steht im Bericht an der Stelle {{ersteller.firma}}; vorbelegt aus der Lizenz.");

    /// <summary>EIN_BERICHT_LBL_VORLAGENORDNER</summary>
    public string LabelVorlagenordner { get; set; } = T("EIN_BERICHT_LBL_VORLAGENORDNER", "Vorlagenordner:");

    /// <summary>EIN_BERICHT_HINT_VORLAGENORDNER — die Herleitung unter dem Ordner.</summary>
    public string HinweisVorlagenordner { get; set; } = T("EIN_BERICHT_HINT_VORLAGENORDNER",
        "Hier liegen die eigenen Berichtsvorlagen. Die Datenbanksicherung nimmt den Ordner nicht mit.");

    /// <summary>
    /// EIN_BERICHT_HINT_GEMEINSAM — der Zusatz, solange der Ordner wählbar ist (unter Windows; auf
    /// iOS liegt er fest in der App).
    /// </summary>
    public string HinweisGemeinsamerOrdner { get; set; } = T("EIN_BERICHT_HINT_GEMEINSAM",
        "Auch ein gemeinsamer Ordner des Büros ist möglich – dann arbeiten alle mit denselben Vorlagen.");

    /// <summary>EIN_BERICHT_NICHT_VERFUEGBAR — die Absage an den Assistenten, wenn der Abschnitt fehlt.</summary>
    public string NichtVerfuegbar { get; set; } = T("EIN_BERICHT_NICHT_VERFUEGBAR",
        "Der Abschnitt „Bericht“ steht in diesem Dialog nicht zur Verfügung.");
}
