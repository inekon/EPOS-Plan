using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Die Beschriftungen einer <see cref="Katalogliste"/></b> — EIN Bündel statt
/// zwölf einzelner <c>[Parameter] string</c> (Hausregel „ab etwa zehn Anzeigetexten
/// ein Bündel").
///
/// <para><b>Es füllt sich SELBST</b> aus <c>MyResource</c> in der
/// Oberflächensprache — dieselbe Bauart wie <c>LizenzTexte</c> (W15c‑O‑2), und aus
/// demselben Grund: Das sind reine Katalogeinträge, keine Werte, die an einer
/// Fallunterscheidung der Hülle hängen. Eine Hülle überschreibt nur, was sie
/// zusammensetzt.</para>
///
/// <para>Ein Bündel trägt <b>Beschriftungen</b>, keinen Zustand — der steht im
/// <see cref="Katalogfilterstand"/>.</para>
/// </summary>
public sealed class Katalogfiltertexte
{
    /// <summary>Beschriftung des einen Suchfeldes (<c>KFLT_SUCHE</c>).</summary>
    public string Suche { get; set; } = Resource.KFLT_SUCHE;

    /// <summary>Platzhalter im Suchfeld (<c>KFLT_SUCHE_PLATZ</c>).</summary>
    public string SuchePlatzhalter { get; set; } = Resource.KFLT_SUCHE_PLATZ;

    /// <summary>
    /// Die Trefferzahl als FORMATSTRING „{0} von {1} Sätzen" (<c>KFLT_TREFFER</c>) —
    /// im Vorläufer stand sie beim Wärmepumpenkatalog im Fenstertitel, den eine
    /// Überlagerung nicht hat (W7‑A‑7).
    /// </summary>
    public string Treffer { get; set; } = Resource.KFLT_TREFFER;

    /// <summary>
    /// Die Trefferzahl ohne Hauptwort „{0} von {1}" (<c>KFLT_TREFFER_KURZ</c>, Konzept 4.10):
    /// Bei Platzmangel in der Kopfleiste faellt das Hauptwort weg, die Zahl kuerzt nie.
    /// </summary>
    public string TrefferKurz { get; set; } = Resource.KFLT_TREFFER_KURZ;

    /// <summary>Der Textknopf „Filter zurücksetzen" (<c>KFLT_ZURUECKSETZEN</c>).</summary>
    public string Zuruecksetzen { get; set; } = Resource.KFLT_ZURUECKSETZEN;

    /// <summary>
    /// „Kein Treffer." statt einer leeren Liste (<c>ETV_SUCHE_LEER</c>) — derselbe
    /// Satz wie im <c>EnergietraegerDialog</c>.
    /// </summary>
    public string KeinTreffer { get; set; } = Resource.ETV_SUCHE_LEER;

    /// <summary>Der Knopf im Popover (<c>KFLT_FILTER_LOESCHEN</c>).</summary>
    public string FilterLoeschen { get; set; } = Resource.KFLT_FILTER_LOESCHEN;

    /// <summary>Platzhalter einer TEXTspalte: „enthält…" (<c>KFLT_PLATZ_TEXT</c>).</summary>
    public string PlatzhalterText { get; set; } = Resource.KFLT_PLATZ_TEXT;

    /// <summary>
    /// Platzhalter und Hinweis einer ZAHLENspalte: „z. B. &gt;10, &lt;60, 10..60, =15"
    /// (<c>KFLT_PLATZ_ZAHL</c>). Er nennt die Formen, damit sie niemand auswendig
    /// können muss (Frage Q1).
    /// </summary>
    public string PlatzhalterZahl { get; set; } = Resource.KFLT_PLATZ_ZAHL;

    /// <summary>Kurztext des Trichters, <c>{0}</c> = Spaltenname (<c>KFLT_TRICHTER</c>).</summary>
    public string Trichter { get; set; } = Resource.KFLT_TRICHTER;

    /// <summary>
    /// Kurztext des GESETZTEN Trichters (<c>KFLT_TRICHTER_GESETZT</c>). Der
    /// Unterschied gefüllt/Umriss trägt ohne Farbe; für die Sprachausgabe braucht er
    /// trotzdem Worte.
    /// </summary>
    public string TrichterGesetzt { get; set; } = Resource.KFLT_TRICHTER_GESETZT;

    /// <summary>Kurztext des Sortierknopfes, <c>{0}</c> = Spaltenname (<c>KFLT_SORTIEREN</c>).</summary>
    public string Sortieren { get; set; } = Resource.KFLT_SORTIEREN;

    /// <summary>Kopf der Wahlspalte (<c>KBROW_SPALTE_WAHL</c> bzw. der Wirt).</summary>
    public string SpalteWahl { get; set; } = "Wahl";

    /// <summary>
    /// <b>Der Kurztext des Schlosses</b> hinter dem Bezeichner eines Auslieferungssatzes
    /// (Konzept Administrationsdialoge, V10) — <c>ADM_SCHLOSS</c>; eine Verwaltung mit
    /// „Duplizieren…" setzt <c>ADM_SCHLOSS_DUPLIZIEREN</c>.
    /// </summary>
    public string Schloss { get; set; } = Resource.ADM_SCHLOSS;

    /// <summary>
    /// <b>Die Beschriftung der Liste als Tabulatorhalt</b> (V4/V11) — sie nennt die
    /// Tasten, weil man ihnen nicht ansieht, dass es sie gibt (<c>ADM_LISTE_TASTEN</c>).
    /// </summary>
    public string Listenbeschriftung { get; set; } = Resource.ADM_LISTE_TASTEN;

    /// <summary>Der Alle-Schalter der Mehrfachwahl (W13‑B‑5) — <c>KFLT_ALLE_WAEHLEN</c>.</summary>
    public string AlleWaehlen { get; set; } = Resource.KFLT_ALLE_WAEHLEN;

    /// <summary>Dieselbe Stelle, wenn schon alle gewählt sind — <c>KFLT_ALLE_ABWAEHLEN</c>.</summary>
    public string AlleAbwaehlen { get; set; } = Resource.KFLT_ALLE_ABWAEHLEN;

    /// <summary>
    /// Kurztext und Name des Kästchens einer Zeile (Stufe 3, V6); <c>{0}</c> = Bezeichner —
    /// <c>ADM_KAESTCHEN_ZEILE</c>.
    /// </summary>
    public string KaestchenZeile { get; set; } = Resource.ADM_KAESTCHEN_ZEILE;

    // =====================================================================
    //  Der VERGLEICH (Frage Q3, Stufe S3.3)
    // =====================================================================

    /// <summary>Der Knopf „Vergleichen" in der Suchzeile (<c>KFLT_VERGLEICHEN</c>).</summary>
    public string Vergleichen { get; set; } = Resource.KFLT_VERGLEICHEN;

    /// <summary>Überschrift der Vergleichs-Überlagerung (<c>KFLT_VERGLEICH_TITEL</c>).</summary>
    public string VergleichTitel { get; set; } = Resource.KFLT_VERGLEICH_TITEL;

    /// <summary>
    /// Der Kurztext am gesperrten Knopf: <b>wie</b> man markiert
    /// (<c>KFLT_VERGLEICH_HINWEIS</c>). Strg-Klick ist unsichtbar; ohne diesen Satz
    /// fände ihn niemand.
    /// </summary>
    public string VergleichHinweis { get; set; } = Resource.KFLT_VERGLEICH_HINWEIS;

    /// <summary>Die Meldung bei der VIERTEN Markierung (<c>KFLT_VERGLEICH_GRENZE</c>).</summary>
    public string VergleichGrenze { get; set; } = Resource.KFLT_VERGLEICH_GRENZE;

    /// <summary>„{0} markiert" — der Zusatz am Knopf (<c>KFLT_VERGLEICH_MARKIERT</c>).</summary>
    public string VergleichMarkiert { get; set; } = Resource.KFLT_VERGLEICH_MARKIERT;

    /// <summary>Kopf der ersten Spalte des Vergleichs (<c>KFLT_VERGLEICH_SP_PARAMETER</c>).</summary>
    public string VergleichSpalteParameter { get; set; } = Resource.KFLT_VERGLEICH_SP_PARAMETER;

    /// <summary>
    /// „abweichend" (<c>KFLT_VERGLEICH_ABWEICHEND</c>) — die Kennzeichnung trägt
    /// WORTE und nicht nur Farbe (dieselbe Auflage wie beim Trichter).
    /// </summary>
    public string VergleichAbweichend { get; set; } = Resource.KFLT_VERGLEICH_ABWEICHEND;

    /// <summary>„stimmig" (<c>KFLT_VERGLEICH_STIMMIG</c>).</summary>
    public string VergleichStimmig { get; set; } = Resource.KFLT_VERGLEICH_STIMMIG;

    /// <summary>Kurztext des ✕ (<c>KFLT_VERGLEICH_SCHLIESSEN</c>).</summary>
    public string VergleichSchliessen { get; set; } = Resource.KFLT_VERGLEICH_SCHLIESSEN;

    /// <summary>Hinweis der Verwendungsmarke (<c>KFLT_VERWENDET_HINWEIS</c>, Konzept 4.10).</summary>
    public string VerwendetHinweis { get; set; } = Resource.KFLT_VERWENDET_HINWEIS;

    /// <summary>Text der Legende neben der Zählung (<c>KFLT_VERWENDET_LEGENDE</c>).</summary>
    public string VerwendetLegende { get; set; } = Resource.KFLT_VERWENDET_LEGENDE;

    /// <summary>„Spalten…“ (<c>KFLT_SPALTEN</c>).</summary>
    public string Spalten { get; set; } = Resource.KFLT_SPALTEN;

    /// <summary>Kurztext des Spaltenknopfs (<c>KFLT_SPALTEN_HINWEIS</c>).</summary>
    public string SpaltenHinweis { get; set; } = Resource.KFLT_SPALTEN_HINWEIS;

    /// <summary>Überschrift der Spaltenwahl (<c>KFLT_SPALTEN_TITEL</c>).</summary>
    public string SpaltenTitel { get; set; } = Resource.KFLT_SPALTEN_TITEL;

    /// <summary>„Standard“ (<c>KFLT_SPALTEN_STANDARD</c>).</summary>
    public string SpaltenStandard { get; set; } = Resource.KFLT_SPALTEN_STANDARD;

    /// <summary>
    /// Der Hinweis neben einer angehakten Spalte, die bei der aktuellen Breite weicht
    /// (<c>KFLT_SPALTE_AUSGEBLENDET</c>, Konzept 4.10).
    /// </summary>
    public string SpalteAusgeblendet { get; set; } = Resource.KFLT_SPALTE_AUSGEBLENDET;

    /// <summary>Kurztext von „Standard“ (<c>KFLT_SPALTEN_STANDARD_HINWEIS</c>).</summary>
    public string SpaltenStandardHinweis { get; set; } = Resource.KFLT_SPALTEN_STANDARD_HINWEIS;
}
