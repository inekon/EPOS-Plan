using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Berichte;

/// <summary>
/// Die Anzeigetexte der Überlagerung „Prüfliste" (<see cref="PrueflisteDialog"/>, BV-E1,
/// Konzept 6.8 und 9.7) — EIN Parameter statt vieler (Hausregel „ab etwa zehn Anzeigetexten
/// ein Bündel").
/// </summary>
/// <remarks>
/// Beschriftungen, kein Zustand. Jede Eigenschaft füllt sich selbst aus <c>MyResource</c> und
/// nennt ihren Schlüssel (<c>VF_PRUEF_*</c>); solange ein Schlüssel fehlt, gilt der deutsche
/// Rückfall — so zeichnet der Dialog auch ohne Gaben. „erklären lassen" nimmt die Texte des
/// Warnbanners (<c>KI_BANNER_ERKLAEREN*</c>), damit der Link überall gleich heißt.
/// </remarks>
public sealed class PrueflisteTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>VF_PRUEF_TITEL</summary>
    public string Titel { get; set; } = T("VF_PRUEF_TITEL", "Prüfliste der Vorlage");

    /// <summary>VF_PRUEF_KOPF — {0} = Zahl der Platzhalter, {1} = Name der Vorlage.</summary>
    public string Kopf { get; set; } = T("VF_PRUEF_KOPF", "Geprüft: {0} Platzhalter, Vorlage „{1}“");

    /// <summary>VF_PRUEF_KOPF_OHNE_NAME — {0} = Zahl der Platzhalter.</summary>
    public string KopfOhneName { get; set; } = T("VF_PRUEF_KOPF_OHNE_NAME", "Geprüft: {0} Platzhalter");

    /// <summary>VF_PRUEF_GRUPPE_FEHLER — {0} = Zahl der Meldungen.</summary>
    public string GruppeFehler { get; set; } = T("VF_PRUEF_GRUPPE_FEHLER", "Fehler ({0})");

    /// <summary>VF_PRUEF_GRUPPE_WARNUNG — {0} = Zahl der Meldungen.</summary>
    public string GruppeWarnung { get; set; } = T("VF_PRUEF_GRUPPE_WARNUNG", "Warnungen ({0})");

    /// <summary>VF_PRUEF_GRUPPE_HINWEIS — {0} = Zahl der Meldungen.</summary>
    public string GruppeHinweis { get; set; } = T("VF_PRUEF_GRUPPE_HINWEIS", "Hinweise ({0})");

    /// <summary>VF_PRUEF_FUNDORT — {0} = der Fundort.</summary>
    public string Fundort { get; set; } = T("VF_PRUEF_FUNDORT", "Fundort: {0}");

    /// <summary>VF_PRUEF_WAS_TUN — {0} = was zu tun ist.</summary>
    public string WasTun { get; set; } = T("VF_PRUEF_WAS_TUN", "Was tun: {0}");

    /// <summary>VF_PRUEF_KEINE_BEFUNDE — die Zeile einer leeren Prüfliste.</summary>
    public string KeineBefunde { get; set; } = T("VF_PRUEF_KEINE_BEFUNDE", "Keine Befunde – die Vorlage lässt sich füllen.");

    /// <summary>VF_PRUEF_LISTE — der Name der Meldungsliste für die Sprachausgabe.</summary>
    public string Liste { get; set; } = T("VF_PRUEF_LISTE", "Meldungen der Prüfung");

    /// <summary>VF_PRUEF_BTN_SCHLIESSEN — der primäre (und einzige) Knopf der Fußleiste.</summary>
    public string Schliessen { get; set; } = T("VF_PRUEF_BTN_SCHLIESSEN", "Schließen");

    /// <summary>KI_BANNER_ERKLAEREN — der Link einer Meldung mit Kennung.</summary>
    public string Erklaeren { get; set; } = T("KI_BANNER_ERKLAEREN", "erklären lassen");

    /// <summary>KI_BANNER_ERKLAEREN_TOOLTIP</summary>
    public string ErklaerenKurztext { get; set; } = T("KI_BANNER_ERKLAEREN_TOOLTIP",
        "Diese Meldung vom Hilfe-Assistenten erklären lassen");
}
