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
}
