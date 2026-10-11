namespace EPOS.UI.Dialoge.Hilfe;

// =====================================================================================
// DIE ANZEIGEFORM VON „UEBER EPOS-PLAN" (Konzept Administration und Support, Abschnitt 5).
// Keine Fachklasse des Kerns: Die Huelle (EPOS.UI.Daten, UeberHuelle) fragt den Kern
// (Diagnosedaten) und die Ressourcen und reicht nur die Ergebnisse herein.
// =====================================================================================

/// <summary>Die Gaben des Dialogs beim Oeffnen.</summary>
/// <param name="Produktname">Der Produktname, etwa „EPOS-Plan".</param>
/// <param name="Claim">Die Zeile unter dem Produktnamen.</param>
/// <param name="Versionszeile">Version mit Baudatum, fertig formatiert.</param>
/// <param name="Herstellerzeile">Die Zeile des Hauses (INEKON).</param>
/// <param name="Diagnosetext">Der Diagnoseblock, je Zeile „Bezeichnung: Wert".</param>
/// <param name="Telefon">Telefonnummer des Supports.</param>
/// <param name="Mail">E-Mail-Adresse des Supports.</param>
/// <param name="Zeiten">Erreichbarkeit in Worten.</param>
public sealed record UeberDaten(string Produktname, string Claim, string Versionszeile, string Herstellerzeile,
                                string Diagnosetext, string Telefon, string Mail, string Zeiten)
{
    /// <summary>Ohne Gaben: alles leer.</summary>
    public static UeberDaten Leer { get; } = new UeberDaten("", "", "", "", "", "", "", "");
}

/// <summary>
/// Das ERGEBNIS von „Supportpaket speichern…" in der Anzeigeform — die Huelle bildet es aus dem
/// Kern-Ergebnis (<c>Supportpaketergebnis</c>). Ohne Pfad und ohne Fehler: Abbruch.
/// </summary>
/// <param name="Pfad">Die geschriebene ZIP-Datei; leer bei Abbruch oder Fehler.</param>
/// <param name="Enthalten">Die Eintraege im Paket.</param>
/// <param name="Fehlend">Die Quellen, die es nicht gab.</param>
/// <param name="Fehler">Leer, wenn das Paket steht; sonst die Meldung des Schreibfehlers.</param>
public sealed record SupportpaketAnzeige(string Pfad, IReadOnlyList<string> Enthalten,
                                         IReadOnlyList<string> Fehlend, string Fehler)
{
    /// <summary>Abbruch im Speicherdialog.</summary>
    public static SupportpaketAnzeige Abgebrochen { get; } =
        new SupportpaketAnzeige("", Array.Empty<string>(), Array.Empty<string>(), "");
}

/// <summary>
/// Der ANSTOSS der Geste des Entwicklermodus (Konzept 3.2, Entwicklerhandbuch Abschnitt 1): Die
/// Schale faengt Strg+Umschalt+F12 im Fenster ab (unter Windows in <c>ProcessCmdKey</c>, bevor
/// die WebView die Taste sieht) und stoesst den Dialog an; der Dialog ruft daraufhin seinen
/// Rueckruf <c>EntwicklermodusSchalten</c> und zeigt den neuen Diagnoseblock. Plattformfrei,
/// ohne Zustand ausser dem einen Empfaenger.
/// </summary>
public sealed class UeberAnstoss
{
    /// <summary>Der Empfaenger — der geoeffnete Dialog; <c>null</c>, solange keiner zeichnet.</summary>
    internal Action? Empfaenger { get; set; }

    /// <summary>Die Geste ist gefallen; ohne geoeffneten Dialog geschieht nichts.</summary>
    public void Ausloesen() => Empfaenger?.Invoke();
}

/// <summary>
/// Das TEXTBUENDEL des Dialogs — Beschriftungen, kein Zustand. Jede Eigenschaft nennt ihren
/// Ressourcenschluessel; der deutsche Text ist der Rueckfall.
/// </summary>
public sealed class UeberTexte
{
    /// <summary><c>HAUPT_UEBER_TITEL</c> mit dem Produktnamen</summary>
    public string Titel { get; init; } = "Über EPOS-Plan";

    /// <summary><c>UEB_ZONE_DIAGNOSE</c></summary>
    public string ZoneDiagnose { get; init; } = "Diagnosedaten";

    /// <summary><c>UEB_DIAGNOSE_HINWEIS</c></summary>
    public string DiagnoseHinweis { get; init; } =
        "Mit diesen Angaben erkennt der Support Ihre Installation. Kopieren Sie sie in Ihre E-Mail " +
        "oder lesen Sie sie am Telefon vor.";

    /// <summary><c>UEB_KOPIEREN</c></summary>
    public string Kopieren { get; init; } = "Diagnosedaten kopieren";

    /// <summary><c>UEB_KOPIERT</c></summary>
    public string Kopiert { get; init; } = "Die Diagnosedaten stehen in der Zwischenablage.";

    /// <summary><c>UEB_PROTOKOLLORDNER</c></summary>
    public string Protokollordner { get; init; } = "Protokollordner öffnen";

    /// <summary><c>UEB_ZONE_KONTAKT</c></summary>
    public string ZoneKontakt { get; init; } = "Kontakt";

    /// <summary><c>UEB_TELEFON</c></summary>
    public string Telefon { get; init; } = "Telefon";

    /// <summary><c>UEB_MAIL</c></summary>
    public string Mail { get; init; } = "E-Mail";

    /// <summary><c>UEB_ZEITEN</c></summary>
    public string Zeiten { get; init; } = "Erreichbar";

    /// <summary><c>UEB_FERNWARTUNG</c></summary>
    public string Fernwartung { get; init; } = "Fernwartung…";

    /// <summary><c>UEB_MAIL_FEHLER</c> — {0} ist die Adresse.</summary>
    public string MailFehler { get; init; } =
        "Das E-Mail-Programm konnte nicht geöffnet werden. Schreiben Sie an {0}.";

    /// <summary><c>UEB_MODUS_GEWECHSELT</c></summary>
    public string ModusGewechselt { get; init; } = "Die Diagnosedaten sind aktualisiert.";

    /// <summary><c>SUP_PAKET_KNOPF</c></summary>
    public string Supportpaket { get; init; } = "Supportpaket speichern…";

    /// <summary><c>SUP_PAKET_HINWEIS</c></summary>
    public string SupportpaketHinweis { get; init; } =
        "Das Supportpaket enthält die Diagnosedaten und die Protokolle des Programms, keine Projektdaten " +
        "und keine Datenbank. Hängen Sie die Datei an Ihre E-Mail an den Support.";

    /// <summary><c>SUP_PAKET_HAKEN</c></summary>
    public string KiProtokoll { get; init; } = "KI-Protokoll beilegen";

    /// <summary><c>SUP_PAKET_HAKEN_HINWEIS</c></summary>
    public string KiProtokollHinweis { get; init; } =
        "Das KI-Protokoll kann Ihre Eingaben an den Assistenten enthalten.";

    /// <summary><c>SUP_PAKET_GESPEICHERT</c> — {0} ist der Pfad.</summary>
    public string SupportpaketGespeichert { get; init; } = "Supportpaket gespeichert: {0}";

    /// <summary><c>SUP_PAKET_ENTHALTEN</c> — {0} ist die Liste der Eintraege.</summary>
    public string SupportpaketEnthalten { get; init; } = "Enthalten: {0}";

    /// <summary><c>SUP_PAKET_FEHLEND</c> — {0} ist die Liste der fehlenden Quellen.</summary>
    public string SupportpaketFehlend { get; init; } = "Nicht vorhanden: {0}";

    /// <summary><c>SUP_PAKET_FEHLER</c> — {0} ist die Meldung.</summary>
    public string SupportpaketFehler { get; init; } = "Das Supportpaket konnte nicht gespeichert werden: {0}";

    /// <summary><c>UEB_SCHLIESSEN</c></summary>
    public string Schliessen { get; init; } = "Schließen";
}
