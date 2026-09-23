using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// ETAPPE E6 — die Beschriftungen des Abschnitts „Verlauf" der Wirtschaftlichkeitsseite
/// (Mockup Kategorie 8, „Der Verlauf über die Zeit — alle drei Szenarien", Bedienleiste des
/// Verlaufs).
///
/// <para>Ein BÜNDEL nach der Bauart <see cref="WirtschaftlichkeitSeiteTexte"/>: Es füllt sich
/// selbst aus <c>MyResource</c> in der Oberflächensprache; ein fehlender Schlüssel fällt auf
/// den deutschen Wortlaut zurück. Jede Eigenschaft nennt ihren Ressourcenschlüssel. Es trägt
/// Beschriftungen, keinen Zustand.</para>
/// </summary>
public sealed class VerlaufTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    /// <summary>WIRT_VERL_TITEL — der Kopf des Abschnitts.</summary>
    public string Titel { get; set; } = T("WIRT_VERL_TITEL", "Der Verlauf über die Zeit — alle drei Szenarien");

    /// <summary>WIRT_VERL_BILD — was das Bild zeigt (Bezeichnung der Zeichenfläche).</summary>
    public string Bild { get; set; }
        = T("WIRT_VERL_BILD", "Kumulierter Barwert der Differenz zur Referenz — drei Szenarien");

    /// <summary>WIRT_VERL_ERKLAERUNG — die leise Zeile unter der Bedienleiste.</summary>
    public string Erklaerung { get; set; } = T("WIRT_VERL_ERKLAERUNG",
        "Jede angehakte Variante bekommt eine Farbe, jedes angehakte Szenario eine Strichart; der "
        + "Nulldurchgang einer Linie ist die dynamische Amortisation in diesem Szenario.");

    /// <summary>WVERL_LBL_ZEITRAUM</summary>
    public string LabelZeitraum { get; set; } = T("WVERL_LBL_ZEITRAUM", "Zeitraum [Jahre]:");

    /// <summary>WIRT_VERL_LEG_VARIANTEN — derselbe Kopf wie in der Legende des Bildes.</summary>
    public string LabelVarianten { get; set; } = T("WIRT_VERL_LEG_VARIANTEN", "Varianten:");

    /// <summary>WIRT_VERL_LEG_SZENARIEN — derselbe Kopf wie in der Legende des Bildes.</summary>
    public string LabelSzenarien { get; set; } = T("WIRT_VERL_LEG_SZENARIEN", "Szenarien:");

    /// <summary>WVERL_BTN_ZEICHNEN — rechnet den Verlauf.</summary>
    public string Aktualisieren { get; set; } = T("WVERL_BTN_ZEICHNEN", "Aktualisieren");

    /// <summary>ALLG_BTN_ABBRECHEN — derselbe Knopf, solange gerechnet wird.</summary>
    public string Abbrechen { get; set; } = T("ALLG_BTN_ABBRECHEN", "Abbrechen");

    /// <summary>WIRT_BTN_VERLAUF_EXCEL (U13)</summary>
    public string NachExcel { get; set; } = T("WIRT_BTN_VERLAUF_EXCEL", "Verlauf nach Excel…");

    /// <summary>WVERL_STATUS_LAEUFT</summary>
    public string Laeuft { get; set; } = T("WVERL_STATUS_LAEUFT", "Berechnung läuft …");

    /// <summary>WVERL_STATUS_ABBRUCH</summary>
    public string Abgebrochen { get; set; } = T("WVERL_STATUS_ABBRUCH", "Vorgang abgebrochen.");

    /// <summary>WVERL_MSG_FEHLER — ein Platzhalter.</summary>
    public string VorlageFehler { get; set; }
        = T("WVERL_MSG_FEHLER", "Fehler beim Berechnen des Verlaufs: {0}");

    /// <summary>WVERL_KEIN_BILD — an der Stelle eines noch nicht gezeichneten Bildes.</summary>
    public string Platzhalter { get; set; } = T("WVERL_KEIN_BILD", "Noch kein Diagramm");
}
