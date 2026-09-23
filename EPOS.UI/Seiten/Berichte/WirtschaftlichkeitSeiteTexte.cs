using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Seiten.Berichte;

/// <summary>
/// ETAPPE E5 Teil b — die Beschriftungen der <b>Ergebnisansicht</b> der
/// Wirtschaftlichkeitsseite: Umschalter „Kennzahlen / ValERI-Bewertung" (U2), die vier
/// Abschnittsköpfe, Karten, Tafeln, der Knopf „Bericht erzeugen" (U44) und die Blöcke der
/// ValERI-Ansicht (Mockup Kategorie 8, Tafel „die fünf Blöcke").
///
/// <para>Ein BÜNDEL nach der Bauart <c>PvModellTexte</c> (Hausregel EPOS.UI: ab etwa zehn
/// Anzeigetexten eines, EIN <c>[Parameter]</c>). Es füllt sich SELBST aus
/// <c>MyResource</c> in der Oberflächensprache; ein fehlender Schlüssel fällt auf den
/// deutschen Wortlaut zurück. Die Hülle muss nichts beisteuern. Jede Eigenschaft nennt
/// ihren Ressourcenschlüssel. Es trägt Beschriftungen, keinen Zustand.</para>
/// </summary>
public sealed class WirtschaftlichkeitSeiteTexte
{
    private static string T(string schluessel, string rueckfall)
    {
        string? t = null;
        try { t = Resource.ResourceManager.GetString(schluessel); }
        catch { }
        return string.IsNullOrEmpty(t) ? rueckfall : t;
    }

    // ---- Umschalter (U2) -----------------------------------------------------

    /// <summary>WIRT_UMSCH_TITEL — die Beschriftung der Knopfgruppe für die Sprachausgabe.</summary>
    public string UmschalterTitel { get; set; } = T("WIRT_UMSCH_TITEL", "Darstellung");
    /// <summary>WIRT_UMSCH_KENNZAHLEN</summary>
    public string UmschalterKennzahlen { get; set; } = T("WIRT_UMSCH_KENNZAHLEN", "Kennzahlen");
    /// <summary>WIRT_UMSCH_VALERI</summary>
    public string UmschalterValeri { get; set; } = T("WIRT_UMSCH_VALERI", "ValERI-Bewertung");

    // ---- Die vier Abschnitte (U2) ----------------------------------------------

    /// <summary>WIRT_ABS_LOHNT</summary>
    public string AbsLohnt { get; set; } = T("WIRT_ABS_LOHNT", "Lohnt es sich?");
    /// <summary>WIRT_ABS_SICHER</summary>
    public string AbsSicher { get; set; } = T("WIRT_ABS_SICHER", "Wie sicher ist das?");
    /// <summary>WIRT_ABS_WORAUS</summary>
    public string AbsWoraus { get; set; } = T("WIRT_ABS_WORAUS", "Woraus entsteht die Zahl?");
    /// <summary>WIRT_ABS_ANNAHMEN</summary>
    public string AbsAnnahmen { get; set; } = T("WIRT_ABS_ANNAHMEN", "Was ist angenommen?");

    // ---- „Lohnt es sich?" (U5) --------------------------------------------------

    /// <summary>WIRT_EMPF_KARTE_UNTER — die leise Zeile unter dem Betrag der Karte.</summary>
    public string KarteUnter { get; set; }
        = T("WIRT_EMPF_KARTE_UNTER", "Kapitalwertdifferenz, Szenario Erwartet");
    /// <summary>WIRT_EMPF_REGEL — die Einstufungsregel unter dem Vorschlag.</summary>
    public string EmpfehlungRegel { get; set; } = T("WIRT_EMPF_REGEL",
        "Einstufung: empfohlen, wenn die Kapitalwertdifferenz in allen drei Szenarien positiv ist · "
        + "bedingt empfohlen, wenn sie im Erwartungsfall positiv, im ungünstigsten der drei Szenarien "
        + "aber nicht positiv ist · nicht empfohlen, wenn sie schon im Erwartungsfall nicht positiv ist.");
    /// <summary>WIRT_KZ_TAFEL_TITEL</summary>
    public string KennzahltafelTitel { get; set; }
        = T("WIRT_KZ_TAFEL_TITEL", "Die Kennzahlen dazu — Szenario Erwartet");

    // ---- „Wie sicher ist das?" (U4) ---------------------------------------------

    /// <summary>WIRT_BB_TITEL</summary>
    public string BandbreiteTitel { get; set; }
        = T("WIRT_BB_TITEL", "Bandbreite der Kapitalwertdifferenz — alle drei Szenarien");
    /// <summary>WIRT_SPANNE_TITEL — die Bezeichnung des Spannenbilds für die Sprachausgabe
    /// (ETAPPE E6, Nachtrag E5b).</summary>
    public string SpanneBild { get; set; }
        = T("WIRT_SPANNE_TITEL", "Spanne der Kapitalwertdifferenz je Version");
    /// <summary>WIRT_SENS_TITEL</summary>
    public string SensitivitaetTitel { get; set; }
        = T("WIRT_SENS_TITEL", "Sensitivität der Kapitalwertdifferenz — Szenario Erwartet");

    // ---- „Was ist angenommen?" (U10) --------------------------------------------

    /// <summary>WIRT_ANN_TITEL</summary>
    public string AnnahmenTitel { get; set; } = T("WIRT_ANN_TITEL", "Annahmen und ihre Herkunft");
    /// <summary>WIRT_LW_TITEL — die Tafel unter dem Hinweistext (ETAPPE E8a, U47).</summary>
    public string LaufwirkungTitel { get; set; } = T("WIRT_LW_TITEL", "Was daraus im Lauf wird");

    // ---- „Bericht erzeugen" (U44) ------------------------------------------------

    /// <summary>WIRT_BTN_BERICHT</summary>
    public string BerichtKnopf { get; set; } = T("WIRT_BTN_BERICHT", "Bericht erzeugen");
    /// <summary>BK_BER_TITEL_ERSTELLEN — der Titel der Rückfragen (derselbe wie auf der Berichtsseite).</summary>
    public string BerichtTitel { get; set; } = T("BK_BER_TITEL_ERSTELLEN", "Bericht erstellen");
    /// <summary>BK_BER_FRAGE_START — die Rückfrage vor dem Lauf; {0} = Zahl der Projekte.</summary>
    public string BerichtFrageStart { get; set; } = T("BK_BER_FRAGE_START",
        "Für diesen Bericht werden {0} Projekt(e) neu simuliert und anschließend wirtschaftlich bewertet.");
    /// <summary>BKS_BTN_JA</summary>
    public string Ja { get; set; } = T("BKS_BTN_JA", "Ja");
    /// <summary>BKS_BTN_NEIN</summary>
    public string Nein { get; set; } = T("BKS_BTN_NEIN", "Nein");

    // ---- Die ValERI-Ansicht (V‑1) -------------------------------------------------

    /// <summary>WIRT_VALERI_BLOCK_1</summary>
    public string Block1 { get; set; } = T("WIRT_VALERI_BLOCK_1", "1 · Gegenstand und Rahmen");
    /// <summary>WIRT_VALERI_BLOCK_2</summary>
    public string Block2 { get; set; } = T("WIRT_VALERI_BLOCK_2", "2 · Zahlungsreihen");
    /// <summary>WIRT_VALERI_BLOCK_3</summary>
    public string Block3 { get; set; } = T("WIRT_VALERI_BLOCK_3", "3 · Kennzahlen");
    /// <summary>WIRT_VALERI_BLOCK_4</summary>
    public string Block4 { get; set; } = T("WIRT_VALERI_BLOCK_4", "4 · Unsicherheit");
    /// <summary>WIRT_VALERI_BLOCK_5</summary>
    public string Block5 { get; set; } = T("WIRT_VALERI_BLOCK_5", "5 · Deklarationen");

    /// <summary>WIRT_VALERI_NORM_1 — der Normbezug als Untertitel.</summary>
    public string Norm1 { get; set; } = T("WIRT_VALERI_NORM_1", "DIN EN 17463 · 6.1 · 7.3");
    /// <summary>WIRT_VALERI_NORM_2</summary>
    public string Norm2 { get; set; } = T("WIRT_VALERI_NORM_2", "DIN EN 17463 · 6.1 bis 6.4");
    /// <summary>WIRT_VALERI_NORM_3</summary>
    public string Norm3 { get; set; } = T("WIRT_VALERI_NORM_3", "DIN EN 17463 · 7 · Anhang C");
    /// <summary>WIRT_VALERI_NORM_4</summary>
    public string Norm4 { get; set; } = T("WIRT_VALERI_NORM_4", "DIN EN 17463 · 7.3 · 8.1.3");
    /// <summary>WIRT_VALERI_NORM_5</summary>
    public string Norm5 { get; set; } = T("WIRT_VALERI_NORM_5", "DIN EN 17463 · 7 · 9 · Anhang E");

    /// <summary>WIRT_VALERI_BLOCK_2_HINWEIS — die EINE Zeile an der Stelle der Zahlungsreihen,
    /// solange in dieser Sitzung kein Lauf Jahresreihen geliefert hat (ETAPPE E8a).</summary>
    public string Block2Hinweis { get; set; } = T("WIRT_VALERI_BLOCK_2_HINWEIS",
        "Die Zahlungsreihen je Jahr stehen nach dem nächsten Rechenlauf („Berechnen“ oder "
        + "„Aktualisieren“ im Verlauf) — die gespeicherten Ergebnisse tragen nur Summen, keine "
        + "Jahresreihen.");
    /// <summary>WIRT_GL_NICHT_GERECHNET — an der Stelle der Gliederung des Kapitalwerts, solange
    /// in dieser Sitzung kein Lauf Jahresreihen geliefert hat (ETAPPE E8a, U46).</summary>
    public string GliederungNichtGerechnet { get; set; } = T("WIRT_GL_NICHT_GERECHNET",
        "Die Gliederung nach Barwert und Nominalsumme je Bestandteil steht nach dem nächsten Rechenlauf "
        + "(„Berechnen“ oder „Aktualisieren“ im Verlauf) — die gespeicherten Ergebnisse tragen keine "
        + "Jahresreihen.");
    /// <summary>WIRT_BR_TITEL — die Bezeichnung des Brückenbilds für die Sprachausgabe (ETAPPE
    /// E8a, U41).</summary>
    public string BrueckeBild { get; set; } = T("WIRT_BR_TITEL", "Von der Investition zur Kapitalwertdifferenz");
    /// <summary>WIRT_ZR_STAND — die Wahl des Standes in Block 2 (ETAPPE E8a).</summary>
    public string ZrStand { get; set; } = T("WIRT_ZR_STAND", "Stand:");
    /// <summary>WIRT_ZR_SZENARIO — die Wahl des Szenarios in Block 2 (ETAPPE E8a).</summary>
    public string ZrSzenario { get; set; } = T("WIRT_ZR_SZENARIO", "Szenario:");
    /// <summary>WIRT_ZS_TITEL — die Bezeichnung des Zahlungsstrombilds in Block 2 für die
    /// Sprachausgabe (ETAPPE E8a, U42).</summary>
    public string ZahlungsstromBild { get; set; } = T("WIRT_ZS_TITEL", "Zahlungsstrom je Jahr");
    /// <summary>WIRT_VALERI_KZ_HINWEIS — die Einordnung der Kennzahlen in Block 3.</summary>
    public string KennzahlHinweis { get; set; } = T("WIRT_VALERI_KZ_HINWEIS",
        "Maß der Vorteilhaftigkeit ist allein der Kapitalwert — als Differenz zur Referenz; die "
        + "Annuität legt ihn auf gleiche Jahresbeträge um. Dynamische Amortisation und interner "
        + "Zinsfuß stehen nachrichtlich (Anhang C).");
    /// <summary>WIRT_DEKL_NOMINAL — die Rechnungsart in Block 1.</summary>
    public string RechnungNominal { get; set; } = T("WIRT_DEKL_NOMINAL", "Rechnung nominal");
    /// <summary>WIRT_NM_ZEILE — die nicht monetären Wirkungen in Block 5; {0} = der gepflegte Text.</summary>
    public string NichtMonetaerZeile { get; set; } = T("WIRT_NM_ZEILE", "Nicht monetäre Wirkungen: {0}");
}
