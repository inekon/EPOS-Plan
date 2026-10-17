namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Die Beschriftungen des Wochenrasters</b> (<see cref="Wochenraster"/>; Konzept
/// Anlagenkopplung 9.2) — ein Bündel statt einzelner Parameter; jede Eigenschaft nennt ihren
/// Ressourcenschlüssel. Es trägt Beschriftungen, keinen Zustand.
/// </summary>
public sealed class WochenrasterTexte
{
    /// <summary><c>WRASTER_TAGE</c> — die sieben Wochentage, Montag zuerst, getrennt durch „;".</summary>
    public string Wochentage { get; set; } = "Mo;Di;Mi;Do;Fr;Sa;So";

    /// <summary><c>WRASTER_KOPF_TAG</c> — Kopf der Tagesspalte.</summary>
    public string KopfTag { get; set; } = "Tag";

    /// <summary><c>WRASTER_ZELLE</c> — Name einer Zelle in Meldung und Tooltip: „{0}" Tag, „{1}" Stunde.</summary>
    public string Zelle { get; set; } = "{0} {1} Uhr";

    /// <summary><c>WRASTER_LBL_ZEILE</c> — die Zeile, die kopiert bzw. gesetzt wird.</summary>
    public string LabelZeile { get; set; } = "Zeile :";

    /// <summary><c>WRASTER_LBL_ZEILENWERT</c> — ein Wert für die ganze Zeile.</summary>
    public string LabelZeilenwert { get; set; } = "Wert für die ganze Zeile :";

    /// <summary><c>WRASTER_BTN_ZEILE_SETZEN</c></summary>
    public string KnopfZeileSetzen { get; set; } = "Zeile setzen";

    /// <summary><c>WRASTER_BTN_WERKTAGE</c></summary>
    public string KnopfWerktage { get; set; } = "auf Mo–Fr kopieren";

    /// <summary><c>WRASTER_BTN_WOCHENENDE</c></summary>
    public string KnopfWochenende { get; set; } = "auf Sa–So kopieren";

    /// <summary><c>WRASTER_BTN_ALLE</c></summary>
    public string KnopfAlle { get; set; } = "auf alle Tage kopieren";

    /// <summary><c>WRASTER_BTN_ANLEGEN</c> — macht aus der Vorgabe ein Zeitprogramm.</summary>
    public string KnopfAnlegen { get; set; } = "Als Zeitprogramm bearbeiten";

    /// <summary><c>WRASTER_BTN_VERWERFEN</c> — verwirft das Zeitprogramm; es gilt wieder die Vorgabe.</summary>
    public string KnopfVerwerfen { get; set; } = "Zeitprogramm verwerfen";

    /// <summary><c>WRASTER_ZEILE_VORGABE</c> — die Zeile über dem Raster ohne Zeitprogramm.</summary>
    public string ZeileVorgabe { get; set; } = "Noch kein Zeitprogramm — das Raster zeigt, was ohne es gilt.";

    /// <summary><c>WRASTER_BILD_TITEL</c> — Titel des Vorschaubilds.</summary>
    public string BildTitel { get; set; } = "Die Woche";

    /// <summary><c>WRASTER_BILD_X</c> — Beschriftung der x-Achse des Vorschaubilds.</summary>
    public string BildAchseX { get; set; } = "Wochenstunde (1..168)";

    /// <summary>
    /// <c>KOND_LBL_AUS</c> — der Zellzustand „aus", nur mit <see cref="Wochenraster.MitAus"/>
    /// (Stufe KP2, Welle U0b). Der Wirt der Konditionierung füllt ihn aus
    /// <c>KonditionierungTexte.ZelleAus</c>.
    /// </summary>
    public string Aus { get; set; } = "aus";

    /// <summary>
    /// <c>KOND_TXT_HINWEIS_AUS</c> — die leise Zeile unter dem Werkzeug, nur mit
    /// <see cref="Wochenraster.MitAus"/>; der Wirt füllt ihn aus <c>KonditionierungTexte.HinweisAus</c>.
    /// </summary>
    public string HinweisAus { get; set; }
        = "„aus“: Die Größe ist in dieser Stunde abgeschaltet (Heizen und Kühlen ohne Betrieb, "
        + "Nutzerlüftung 0 1/h, Anteile 0 %). Ein neuer Wert in der Zelle hebt „aus“ wieder auf.";

    /// <summary>Die sieben Tagesnamen — aus <see cref="Wochentage"/>, notfalls die Zahlen 1 bis 7.</summary>
    public IReadOnlyList<string> Tagesnamen
    {
        get
        {
            string[] teile = (Wochentage ?? "").Split(';');
            if (teile.Length == 7) return teile;
            return new[] { "1", "2", "3", "4", "5", "6", "7" };
        }
    }
}
