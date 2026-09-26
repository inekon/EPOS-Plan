using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// Die Anzeigetexte der Platzhalteranzeige (Konzept Berichtsvorlagen 9.4, 9.7): Marke
/// (<see cref="Vorlagenfeldknopf"/>, <c>VF_KNOPF_*</c>), Umschalter und leise Zeile
/// (<see cref="Vorlagenfeldumschalter"/>, <see cref="Vorlagenfeldzeile"/>, <c>VF_ANZEIGE_*</c>) — EIN
/// Bündel statt vieler Parameter.
/// </summary>
/// <remarks>
/// Beschriftungen, kein Zustand. Jede Eigenschaft füllt sich selbst aus <c>MyResource</c> und nennt
/// ihren Schlüssel; solange einer fehlt, gilt der deutsche Rückfall — so zeichnen die Bausteine auch
/// ohne Gaben. Die Hinweise zum Blockrahmen tragen geschweifte Klammern als Text und gehen durch kein
/// <c>string.Format</c>.
/// </remarks>
public sealed class VorlagenfeldTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    // ---- Die Marke ---------------------------------------------------------------------

    /// <summary>VF_KNOPF_ARIA_KOPIEREN — {0} = Schlüssel; Name der Marke in der Stellung „Schlüssel".</summary>
    public string AriaKopieren { get; set; } = T("VF_KNOPF_ARIA_KOPIEREN", "Platzhalter {0} kopieren");

    /// <summary>VF_KNOPF_ARIA_ZEIGEN — {0} = Schlüssel; Name der Marke in der Stellung „Marken".</summary>
    public string AriaZeigen { get; set; } = T("VF_KNOPF_ARIA_ZEIGEN", "Platzhalter {0}: Angaben zeigen und kopieren");

    /// <summary>VF_KNOPF_TITEL — {0} = Schlüssel, {1} = Kurzbeschreibung; das Mouse-over der Marke.</summary>
    public string Titel { get; set; } = T("VF_KNOPF_TITEL", "{0} – {1}");

    /// <summary>VF_KNOPF_AUFKLAPPUNG — {0} = Schlüssel; der Name der Aufklappung für die Sprachausgabe.</summary>
    public string Aufklappung { get; set; } = T("VF_KNOPF_AUFKLAPPUNG", "Platzhalter {0}");

    /// <summary>VF_KNOPF_ART</summary>
    public string Art { get; set; } = T("VF_KNOPF_ART", "Art");

    /// <summary>VF_KNOPF_KONTEXT</summary>
    public string Kontext { get; set; } = T("VF_KNOPF_KONTEXT", "Kontext");

    /// <summary>VF_KNOPF_BEISPIEL</summary>
    public string Beispiel { get; set; } = T("VF_KNOPF_BEISPIEL", "Beispiel");

    /// <summary>VF_KNOPF_EINHEIT</summary>
    public string Einheit { get; set; } = T("VF_KNOPF_EINHEIT", "Einheit");

    /// <summary>VF_KNOPF_LEER — die Beschriftung des Leerwerts.</summary>
    public string Leer { get; set; } = T("VF_KNOPF_LEER", "ohne Wert");

    /// <summary>VF_KNOPF_LEER_BLEIBT — steht statt des Leerwerts, wenn er leer ist.</summary>
    public string LeerBleibt { get; set; } = T("VF_KNOPF_LEER_BLEIBT", "bleibt leer");

    /// <summary>VF_KNOPF_EXCEL</summary>
    public string Excel { get; set; } = T("VF_KNOPF_EXCEL", "Excel");

    /// <summary>VF_KNOPF_NICHT_EXCEL — der Platzhalter gehört nur in den Word-Bericht.</summary>
    public string NichtExcel { get; set; } = T("VF_KNOPF_NICHT_EXCEL", "nur im Word-Bericht");

    /// <summary>VF_KNOPF_STUFE_ENTSPRICHT</summary>
    public string StufeEntspricht { get; set; } = T("VF_KNOPF_STUFE_ENTSPRICHT", "entspricht dem Bericht");

    /// <summary>VF_KNOPF_STUFE_AEHNLICH</summary>
    public string StufeAehnlich { get; set; } = T("VF_KNOPF_STUFE_AEHNLICH", "ähnlich im Bericht");

    /// <summary>VF_KNOPF_ZUSATZ — die Beschriftung der Zeilen- und Spaltenschlüssel einer Tabelle.</summary>
    public string Zusatz { get; set; } = T("VF_KNOPF_ZUSATZ", "Zeilen und Spalten");

    /// <summary>VF_KNOPF_HINWEIS_EINZELN</summary>
    public string HinweisEinzeln { get; set; } = T("VF_KNOPF_HINWEIS_EINZELN",
        "In Word tippen oder einfügen; Absatz- und Zeichenformat der Stelle bleiben.");

    /// <summary>VF_KNOPF_HINWEIS_ABSATZ</summary>
    public string HinweisAbsatz { get; set; } = T("VF_KNOPF_HINWEIS_ABSATZ",
        "In einen eigenen Absatz einfügen – der Absatz wird durch den Inhalt ersetzt.");

    /// <summary>VF_KNOPF_HINWEIS_BILD</summary>
    public string HinweisBild { get; set; } = T("VF_KNOPF_HINWEIS_BILD",
        "Bild einfügen, Alternativtext = Schlüssel.");

    /// <summary>VF_KNOPF_HINWEIS_SCHALTER</summary>
    public string HinweisSchalter { get; set; } = T("VF_KNOPF_HINWEIS_SCHALTER",
        "Nur als Bedingung: den Bereich zwischen die beiden Zeilen setzen.");

    /// <summary>VF_KNOPF_HINWEIS_STAND — Text, kein Formatmuster.</summary>
    public string HinweisStand { get; set; } = T("VF_KNOPF_HINWEIS_STAND",
        "Wert je Stand – gilt nur im Block {{#je stand}} … {{/je}}; der Rahmen wird mitkopiert.");

    /// <summary>VF_KNOPF_HINWEIS_STAND_BILD — Text, kein Formatmuster.</summary>
    public string HinweisStandBild { get; set; } = T("VF_KNOPF_HINWEIS_STAND_BILD",
        "Bild je Stand – das Bild zwischen {{#je stand}} und {{/je}} setzen.");

    /// <summary>VF_KNOPF_HINWEIS_GEBAEUDE — Text, kein Formatmuster.</summary>
    public string HinweisGebaeude { get; set; } = T("VF_KNOPF_HINWEIS_GEBAEUDE",
        "Wert je Gebäude – gilt nur im Block {{#je gebaeude}} … {{/je}}; der Rahmen wird mitkopiert.");

    /// <summary>VF_KNOPF_HINWEIS_GEBAEUDE_BILD — Text, kein Formatmuster.</summary>
    public string HinweisGebaeudeBild { get; set; } = T("VF_KNOPF_HINWEIS_GEBAEUDE_BILD",
        "Bild je Gebäude – das Bild zwischen {{#je gebaeude}} und {{/je}} setzen.");

    /// <summary>VF_KNOPF_KOPIEREN</summary>
    public string Kopieren { get; set; } = T("VF_KNOPF_KOPIEREN", "Kopieren");

    /// <summary>VF_KNOPF_KOPIERT — die Rückmeldung für 1,5 s.</summary>
    public string Kopiert { get; set; } = T("VF_KNOPF_KOPIERT", "kopiert");

    /// <summary>VF_KNOPF_AUSBLENDEN</summary>
    public string Ausblenden { get; set; } = T("VF_KNOPF_AUSBLENDEN", "Platzhalter ausblenden");

    /// <summary>VF_KNOPF_LBL_TEXT — die Beschriftung des markierten Feldes ohne Zwischenablage.</summary>
    public string LabelText { get; set; } = T("VF_KNOPF_LBL_TEXT", "Text für die Vorlage");

    /// <summary>VF_KNOPF_OHNE_ABLAGE</summary>
    public string OhneAblage { get; set; } = T("VF_KNOPF_OHNE_ABLAGE",
        "Die Zwischenablage ist hier nicht erreichbar – der Text ist markiert und lässt sich selbst kopieren.");

    /// <summary>VF_KNOPF_ESC</summary>
    public string Esc { get; set; } = T("VF_KNOPF_ESC", "Esc schließt");

    // ---- Umschalter und Zeile ----------------------------------------------------------

    /// <summary>VF_ANZEIGE_TITEL — <c>title</c> und Name des Umschalters.</summary>
    public string Umschalter { get; set; } = T("VF_ANZEIGE_TITEL", "Platzhalter zeigen");

    /// <summary>VF_ANZEIGE_AUS</summary>
    public string StellungAus { get; set; } = T("VF_ANZEIGE_AUS", "Aus");

    /// <summary>VF_ANZEIGE_MARKEN</summary>
    public string StellungMarken { get; set; } = T("VF_ANZEIGE_MARKEN", "Marken");

    /// <summary>VF_ANZEIGE_SCHLUESSEL</summary>
    public string StellungSchluessel { get; set; } = T("VF_ANZEIGE_SCHLUESSEL", "Schlüssel");

    /// <summary>VF_ANZEIGE_ZEILE — {0} = Zahl der Platzhalter; die leise Zeile der Stellung „Schlüssel".</summary>
    public string Zeile { get; set; } = T("VF_ANZEIGE_ZEILE",
        "{0} Platzhalter auf dieser Seite · ein Klick auf einen Schlüssel kopiert ihn");

    /// <summary>VF_ANZEIGE_KATALOG — öffnet den Platzhalterkatalog.</summary>
    public string Katalog { get; set; } = T("VF_ANZEIGE_KATALOG", "Katalog…");
}
