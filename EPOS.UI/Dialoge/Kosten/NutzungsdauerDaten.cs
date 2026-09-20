namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Eine Zeile der Nutzungsdauertabelle, wie der Dialog sie zeigt und bearbeitet
/// (Konzept „Nutzungsdauer je Technik und Positionsart", Stufe S1).
///
/// <para><b>Veränderlich, nicht als Record.</b> Die Eingaben leben bis „Speichern"
/// im Objekt (Hausregel der Kostendialoge Ä12): Der Dialog schreibt in diese
/// Felder, und erst der Speicherweg reicht sie an die Hülle weiter.</para>
/// </summary>
public sealed class NutzungsdauerZeileAnzeige
{
    /// <summary>Schlüssel der Zeile (<c>Tab_Nutzungsdauer.ID</c>).</summary>
    public int Id { get; set; }

    /// <summary>Technik (Kostenkomponente); <c>null</c> = technikübergreifend.</summary>
    public int? TechnikId { get; set; }

    /// <summary>Anzeigename der Technik; leer bei den technikübergreifenden Zeilen.</summary>
    public string Technik { get; set; } = "";

    /// <summary>Positionsart — der zweite Teil des Schlüssels.</summary>
    public string Positionsart { get; set; } = "";

    /// <summary>Standardzeile ihrer Technik: Sie gilt ohne Positionsart.</summary>
    public bool IstStandard { get; set; }

    /// <summary>Rechnerische Nutzungsdauer [a]; <c>null</c> = wie die Standardzeile.</summary>
    public double? Nutzungsdauer { get; set; }

    /// <summary>Steuerliche Nutzungsdauer [a] — nur Anzeige.</summary>
    public double? AfaSteuerlich { get; set; }

    /// <summary>Quelle des Werts, als Text.</summary>
    public string Quelle { get; set; } = "";

    /// <summary>Auslieferungszeile: Wert änderbar, Zeile nicht löschbar.</summary>
    public bool Auslieferung { get; set; }

    /// <summary>Eine flache Kopie — der Stand, mit dem der Dialog aufgemacht hat.</summary>
    public NutzungsdauerZeileAnzeige Kopie()
    {
        return new NutzungsdauerZeileAnzeige
        {
            Id = Id,
            TechnikId = TechnikId,
            Technik = Technik,
            Positionsart = Positionsart,
            IstStandard = IstStandard,
            Nutzungsdauer = Nutzungsdauer,
            AfaSteuerlich = AfaSteuerlich,
            Quelle = Quelle,
            Auslieferung = Auslieferung,
        };
    }

    /// <summary>Unterscheiden sich die eingebbaren Felder von <paramref name="andere"/>?</summary>
    public bool WeichtAb(NutzungsdauerZeileAnzeige? andere)
    {
        if (andere is null) return true;
        return Nutzungsdauer != andere.Nutzungsdauer
            || AfaSteuerlich != andere.AfaSteuerlich
            || !string.Equals(Positionsart, andere.Positionsart, System.StringComparison.Ordinal)
            || !string.Equals(Quelle, andere.Quelle, System.StringComparison.Ordinal);
    }
}

/// <summary>Die Angaben einer NEUEN Zeile — was der Anwender in der Neuzeile einträgt.</summary>
/// <param name="TechnikId">Technik; <c>null</c> = technikübergreifend.</param>
/// <param name="Positionsart">Positionsart, Pflichtangabe.</param>
/// <param name="Nutzungsdauer">Nutzungsdauer [a]; <c>null</c> = wie die Standardzeile.</param>
/// <param name="AfaSteuerlich">Steuerliche Nutzungsdauer [a]; <c>null</c> = keine.</param>
public readonly record struct NutzungsdauerNeuEingabe(int? TechnikId, string Positionsart,
                                                      double? Nutzungsdauer,
                                                      double? AfaSteuerlich);

/// <summary>
/// Das Ergebnis des Dialogs: Wurde mit OK geschlossen, und hat sich etwas geändert?
/// <c>null</c> als Rückgabe heißt „Abbrechen" (Hausregel der Dialoge).
/// </summary>
/// <param name="Geaendert">Es wurde mindestens eine Zeile geschrieben.</param>
public readonly record struct NutzungsdauerErgebnis(bool Geaendert);

/// <summary>
/// Das TEXTBÜNDEL des Dialogs (Hausregel: ab etwa zehn Anzeigetexten ein Bündel
/// statt einzelner Parameter). Es trägt nur Beschriftungen, keinen Zustand; jede
/// Eigenschaft nennt ihren Ressourcenschlüssel im Kommentar.
/// </summary>
public sealed class NutzungsdauerTexte
{
    /// <summary><c>ND_TITEL</c></summary>
    public string Titel { get; set; } = "Nutzungsdauern (AfA)";

    /// <summary><c>ND_KONTEXT</c></summary>
    public string Kontext { get; set; } =
        "Rechnerische Nutzungsdauer je Technik und Positionsart — sie steuert " +
        "Ersatzbeschaffung und Restwert im Kapitalwert.";

    /// <summary><c>ND_SP_TECHNIK</c></summary>
    public string SpalteTechnik { get; set; } = "Technik";

    /// <summary><c>ND_SP_POSITIONSART</c></summary>
    public string SpaltePositionsart { get; set; } = "Positionsart";

    /// <summary><c>ND_SP_NUTZUNGSDAUER</c></summary>
    public string SpalteNutzungsdauer { get; set; } = "Nutzungsdauer [a]";

    /// <summary><c>ND_SP_AFA</c></summary>
    public string SpalteAfa { get; set; } = "AfA steuerlich [a]";

    /// <summary><c>ND_SP_QUELLE</c></summary>
    public string SpalteQuelle { get; set; } = "Quelle";

    /// <summary><c>ND_SP_AKTIONEN</c></summary>
    public string SpalteAktionen { get; set; } = "Aktionen";

    /// <summary><c>ND_SUCHE</c></summary>
    public string Suche { get; set; } = "Suchen";

    /// <summary><c>ND_SUCHE_PLATZHALTER</c></summary>
    public string SuchePlatzhalter { get; set; } = "Technik, Positionsart oder Quelle";

    /// <summary><c>ND_NEU</c></summary>
    public string Neu { get; set; } = "Neu";

    /// <summary><c>ND_LOESCHEN</c></summary>
    public string Loeschen { get; set; } = "Löschen";

    /// <summary><c>ND_WIEDERHERSTELLEN</c></summary>
    public string Wiederherstellen { get; set; } = "Auslieferungswerte wiederherstellen";

    /// <summary><c>ND_FRAGE_WIEDERHERSTELLEN</c></summary>
    public string FrageWiederherstellen { get; set; } =
        "Die Werte aller Auslieferungszeilen auf die Auslieferung zurücksetzen? " +
        "Eigene Zeilen bleiben unberührt.";

    /// <summary><c>ND_FRAGE_LOESCHEN</c> — Platzhalter {0} ist die Positionsart.</summary>
    public string FrageLoeschen { get; set; } = "Zeile „{0}“ löschen?";

    /// <summary><c>ND_TIP_AUSLIEFERUNG</c></summary>
    public string TipAuslieferung { get; set; } =
        "Auslieferungszeile: Der Wert ist änderbar, die Zeile wird nicht gelöscht.";

    /// <summary><c>ND_TIP_STANDARD</c></summary>
    public string TipStandard { get; set; } =
        "Standardzeile der Technik — sie gilt, wenn eine Position keine Positionsart trägt.";

    /// <summary><c>ND_KENNZEICHEN_STANDARD</c></summary>
    public string KennzeichenStandard { get; set; } = "Standard";

    /// <summary><c>ND_TECHNIKUEBERGREIFEND</c></summary>
    public string Technikuebergreifend { get; set; } = "technikübergreifend";

    /// <summary><c>ND_LEER_HINWEIS</c></summary>
    public string LeerHinweis { get; set; } =
        "Eine leere Nutzungsdauer heißt „wie die Standardzeile der Technik“.";

    /// <summary><c>ND_LEER_LISTE</c></summary>
    public string LeereListe { get; set; } = "Keine Zeile passt zur Suche.";

    /// <summary><c>ND_MELD_ART_LEER</c></summary>
    public string MeldungArtLeer { get; set; } = "Die Positionsart darf nicht leer sein.";

    /// <summary><c>ALLG_BTN_OK</c></summary>
    public string Ok { get; set; } = "OK";

    /// <summary>Hausknopf <c>ALLG_BTN_ABBRECHEN</c> statt eines eigenen,
    /// gleichlautenden Schlüssels je Dialog (W-E2, Mockup-Prüfung 04).</summary>
    public string Abbrechen { get; set; } = "Abbrechen";

    /// <summary><c>ADM_BTN_SPEICHERN</c></summary>
    public string Speichern { get; set; } = "Speichern";
}
