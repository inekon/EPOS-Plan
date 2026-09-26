using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Berichte;

/// <summary>
/// Die Anzeigetexte der Überlagerung „Platzhalterkatalog" (<see cref="PlatzhalterkatalogDialog"/>,
/// BV-E1, Konzept 9.1 D und 9.7) — EIN Parameter statt vieler (Hausregel „ab etwa zehn
/// Anzeigetexten ein Bündel").
/// </summary>
/// <remarks>
/// Beschriftungen, kein Zustand. Jede Eigenschaft füllt sich selbst aus <c>MyResource</c> und
/// nennt ihren Schlüssel (<c>VF_KATALOG_*</c>); solange ein Schlüssel fehlt, gilt der deutsche
/// Rückfall — so zeichnet der Dialog auch ohne Gaben.
/// </remarks>
public sealed class PlatzhalterkatalogTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>VF_KATALOG_TITEL</summary>
    public string Titel { get; set; } = T("VF_KATALOG_TITEL", "Platzhalterkatalog");

    /// <summary>VF_KATALOG_HINWEIS — die leise Zeile über der Suche.</summary>
    public string Hinweis { get; set; } = T("VF_KATALOG_HINWEIS",
        "Alle Platzhalter, die eine Word-Vorlage tragen kann. Eine Zeile wählen: Darunter steht der Platzhalter in der Schreibweise der Vorlage.");

    /// <summary>VF_KATALOG_LBL_SUCHE</summary>
    public string LabelSuche { get; set; } = T("VF_KATALOG_LBL_SUCHE", "Suche:");

    /// <summary>VF_KATALOG_SUCHE_PLATZHALTER — der graue Text im leeren Suchfeld.</summary>
    public string SuchePlatzhalter { get; set; } = T("VF_KATALOG_SUCHE_PLATZHALTER", "Schlüssel oder Beschreibung");

    /// <summary>VF_KATALOG_LISTE — der Name der Tabelle für die Sprachausgabe.</summary>
    public string Liste { get; set; } = T("VF_KATALOG_LISTE", "Platzhalter");

    /// <summary>VF_KATALOG_SP_SCHLUESSEL</summary>
    public string SpalteSchluessel { get; set; } = T("VF_KATALOG_SP_SCHLUESSEL", "Schlüssel");

    /// <summary>VF_KATALOG_SP_ART</summary>
    public string SpalteArt { get; set; } = T("VF_KATALOG_SP_ART", "Art");

    /// <summary>VF_KATALOG_SP_KONTEXT</summary>
    public string SpalteKontext { get; set; } = T("VF_KATALOG_SP_KONTEXT", "Kontext");

    /// <summary>VF_KATALOG_SP_BESCHREIBUNG</summary>
    public string SpalteBeschreibung { get; set; } = T("VF_KATALOG_SP_BESCHREIBUNG", "Beschreibung");

    /// <summary>VF_KATALOG_ZEILE_WAEHLEN — {0} = Schlüssel; der Name des Wahlknopfs einer Zeile.</summary>
    public string ZeileWaehlen { get; set; } = T("VF_KATALOG_ZEILE_WAEHLEN", "Platzhalter {0} wählen");

    /// <summary>VF_KATALOG_ANZAHL — {0} = Zahl der Einträge ohne Suche.</summary>
    public string Anzahl { get; set; } = T("VF_KATALOG_ANZAHL", "{0} Platzhalter");

    /// <summary>VF_KATALOG_ANZAHL_GEFILTERT — {0} = Treffer, {1} = alle.</summary>
    public string AnzahlGefiltert { get; set; } = T("VF_KATALOG_ANZAHL_GEFILTERT", "{0} von {1} Platzhaltern");

    /// <summary>VF_KATALOG_KEIN_TREFFER</summary>
    public string KeinTreffer { get; set; } = T("VF_KATALOG_KEIN_TREFFER", "Kein Platzhalter passt zur Suche.");

    /// <summary>VF_KATALOG_LEER</summary>
    public string Leer { get; set; } = T("VF_KATALOG_LEER", "Der Katalog ist leer.");

    /// <summary>VF_KATALOG_LBL_SCHREIBWEISE — die Beschriftung des nur lesbaren Feldes.</summary>
    public string LabelSchreibweise { get; set; } = T("VF_KATALOG_LBL_SCHREIBWEISE", "Schreibweise in der Vorlage:");

    /// <summary>VF_KATALOG_HINT_SCHREIBWEISE — die Zeile unter dem Feld.</summary>
    public string HinweisSchreibweise { get; set; } = T("VF_KATALOG_HINT_SCHREIBWEISE",
        "Der Platzhalter ist markiert – kopieren und an der gewünschten Stelle der Vorlage einfügen.");

    /// <summary>VF_KATALOG_BEISPIEL — {0} = die Beispielausgabe.</summary>
    public string Beispiel { get; set; } = T("VF_KATALOG_BEISPIEL", "Beispiel: {0}");

    /// <summary>VF_KATALOG_BTN_BAUKASTEN — „Baukasten speichern…" links in der Fußleiste.</summary>
    public string BaukastenSpeichern { get; set; } = T("VF_KATALOG_BTN_BAUKASTEN", "Baukasten speichern…");

    /// <summary>VF_KATALOG_BTN_SCHLIESSEN — der primäre (und einzige) Knopf der Fußleiste.</summary>
    public string Schliessen { get; set; } = T("VF_KATALOG_BTN_SCHLIESSEN", "Schließen");
}
