namespace EPOS.UI.Dialoge.Hilfe;

// =====================================================================================
// DIE ANZEIGEFORM DER FERNWARTUNG (Konzept Administration und Support, Abschnitt 4.2).
// Keine Fachklasse des Kerns: Die Huelle (EPOS.UI.Daten, FernwartungHuelle) fragt den Kern
// (LizenzCtrl.FernwartungErlaubt, Diagnosedaten) und reicht nur die Ergebnisse herein.
// =====================================================================================

/// <summary>Die Gaben des Dialogs beim Oeffnen.</summary>
/// <param name="Erlaubt">Ist der Knopf „Fernwartungsseite oeffnen" frei?</param>
/// <param name="Sperrgrund">Warum nicht — leer, wenn erlaubt.</param>
/// <param name="Telefon">Telefonnummer des Supports.</param>
/// <param name="Mail">E-Mail-Adresse des Supports.</param>
/// <param name="Zeiten">Erreichbarkeit in Worten.</param>
/// <param name="Diagnosetext">Der Diagnoseblock, je Zeile „Bezeichnung: Wert".</param>
/// <param name="Seite">Adresse der Fernwartungsseite.</param>
public sealed record FernwartungDaten(bool Erlaubt, string Sperrgrund, string Telefon, string Mail,
                                      string Zeiten, string Diagnosetext, string Seite)
{
    /// <summary>Ohne Gaben: gesperrt, ohne Angaben.</summary>
    public static FernwartungDaten Leer { get; } = new FernwartungDaten(false, "", "", "", "", "", "");
}

/// <summary>
/// Das TEXTBUENDEL des Dialogs — Beschriftungen, kein Zustand. Jede Eigenschaft nennt ihren
/// Ressourcenschluessel; der deutsche Text ist der Rueckfall.
/// </summary>
public sealed class FernwartungTexte
{
    /// <summary><c>FW_TITEL</c></summary>
    public string Titel { get; init; } = "Fernwartung";

    /// <summary><c>FW_ZONE_KONTAKT</c></summary>
    public string ZoneKontakt { get; init; } = "Kontakt";

    /// <summary><c>FW_TELEFON</c></summary>
    public string Telefon { get; init; } = "Telefon";

    /// <summary><c>FW_MAIL</c></summary>
    public string Mail { get; init; } = "E-Mail";

    /// <summary><c>FW_ZEITEN</c></summary>
    public string Zeiten { get; init; } = "Erreichbar";

    /// <summary><c>FW_ZONE_ANGABEN</c></summary>
    public string ZoneAngaben { get; init; } = "Ihre Angaben für den Support";

    /// <summary><c>FW_ANGABEN_HINWEIS</c></summary>
    public string AngabenHinweis { get; init; } =
        "Mit diesen Angaben erkennt der Support Ihre Installation. Kopieren Sie sie in Ihre E-Mail " +
        "oder lesen Sie sie am Telefon vor.";

    /// <summary><c>FW_KOPIEREN</c></summary>
    public string Kopieren { get; init; } = "Kopieren";

    /// <summary><c>FW_KOPIERT</c></summary>
    public string Kopiert { get; init; } = "Die Angaben stehen in der Zwischenablage.";

    /// <summary><c>FW_ZONE_FERNWARTUNG</c></summary>
    public string ZoneFernwartung { get; init; } = "Fernwartung";

    /// <summary><c>FW_ERKLAERUNG</c></summary>
    public string Erklaerung { get; init; } =
        "Bei der Fernwartung sieht ein Mitarbeiter des Supports Ihren Bildschirm und kann Ihnen die " +
        "Bedienung zeigen oder einen Fehler mit Ihnen beheben. Den Bildschirm sieht nur der Support, mit " +
        "dem Sie gerade sprechen; EPOS-Plan zeichnet dabei nichts auf. Jede Verbindung nehmen Sie im " +
        "Fernwartungsprogramm selbst an, und Sie können sie dort jederzeit beenden.";

    /// <summary><c>FW_SCHRITT_1</c></summary>
    public string Schritt1 { get; init; } =
        "Laden Sie das Fernwartungsprogramm von der Fernwartungsseite und starten Sie es.";

    /// <summary><c>FW_SCHRITT_2</c></summary>
    public string Schritt2 { get; init; } =
        "Nennen Sie dem Support die angezeigte Kennung und das Einmalkennwort.";

    /// <summary><c>FW_SCHRITT_3</c></summary>
    public string Schritt3 { get; init; } = "Nehmen Sie die Anfrage im Programm an.";

    /// <summary><c>FW_SEITE_OEFFNEN</c></summary>
    public string SeiteOeffnen { get; init; } = "Fernwartungsseite öffnen";

    /// <summary><c>FW_SEITE_FEHLER</c> — {0} ist die Adresse.</summary>
    public string SeiteFehler { get; init; } =
        "Die Fernwartungsseite konnte nicht geöffnet werden. Öffnen Sie {0} in Ihrem Browser oder rufen Sie uns an.";

    /// <summary><c>FW_SCHLIESSEN</c></summary>
    public string Schliessen { get; init; } = "Schließen";
}
