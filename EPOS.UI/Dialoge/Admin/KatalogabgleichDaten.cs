using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Admin;

// =====================================================================================
// DIE ANZEIGEFORM DES KATALOGABGLEICHS (Entscheidungsvorlage Modellgrenzen KU1 Stufe 1).
// Keine Fachklasse des Kerns: Die Hülle (EPOS.UI.Daten, KatalogabgleichHuelle) übersetzt
// das Ergebnis des Kerns in diese Records.
// =====================================================================================

/// <summary>Eine Zeile der Liste: ein Satz mit seiner Aktion.</summary>
/// <param name="Tabelle">Die Katalogtabelle (Schlüssel für das Wiederherstellen).</param>
/// <param name="Katalog">Der Katalog in Worten.</param>
/// <param name="Schluessel">Der Katalogschlüssel des Satzes.</param>
/// <param name="Satz">Der Bezeichner.</param>
/// <param name="Aktion">Die Aktion in Worten.</param>
/// <param name="Hinweis">Der Hinweis für den Anwender.</param>
/// <param name="Wiederherstellbar">Kann der Auslieferungsstand wiederhergestellt werden?</param>
public sealed record KatalogabgleichZeile(string Tabelle, string Katalog, string Schluessel, string Satz,
                                          string Aktion, string Hinweis, bool Wiederherstellbar);

/// <summary>Der Stand beim Öffnen.</summary>
/// <param name="FassungDatenbank">Die Katalogfassung der Datenbank in Worten.</param>
/// <param name="FassungPaket">Die Fassung des Pakets in Worten (oder „kein Paket").</param>
/// <param name="Paketpfad">Wo das Paket gesucht wurde.</param>
/// <param name="PaketVorhanden">Liegt ein lesbares Paket vor?</param>
/// <param name="Protokoll">Die jüngsten Zeilen des Protokolls, je eine Textzeile.</param>
public sealed record KatalogabgleichStand(string FassungDatenbank, string FassungPaket, string Paketpfad,
                                          bool PaketVorhanden, IReadOnlyList<string> Protokoll)
{
    /// <summary>Der Stand ohne Gaben.</summary>
    public static KatalogabgleichStand Leer { get; } =
        new KatalogabgleichStand("—", "—", "", false, new List<string>());
}

/// <summary>Die Antwort auf „Nur prüfen", „Abgleichen" oder „Wiederherstellen".</summary>
/// <param name="Ok">Ist der Schritt gelungen?</param>
/// <param name="Meldung">Was der Anwender lesen soll (leer = nichts).</param>
/// <param name="Zusammenfassung">„n neu, m aktualisiert, k behalten, a ausgelaufen" (leer ohne Lauf).</param>
/// <param name="Zeilen">Die Sätze mit Aktion.</param>
/// <param name="Stand">Der neue Stand (Fassung, Protokoll); <c>null</c> = unverändert.</param>
public sealed record KatalogabgleichAntwort(bool Ok, string Meldung, string Zusammenfassung,
                                            IReadOnlyList<KatalogabgleichZeile> Zeilen,
                                            KatalogabgleichStand? Stand = null);

/// <summary>
/// Das TEXTBÜNDEL des Dialogs — Beschriftungen, kein Zustand. Jede Eigenschaft nennt ihren
/// Ressourcenschlüssel; der deutsche Text ist der Rückfall.
/// </summary>
public sealed class KatalogabgleichTexte
{
    /// <summary><c>KABG_TITEL</c></summary>
    public string Titel { get; init; } = "Katalog aktualisieren";

    /// <summary><c>KABG_EINLEITUNG</c></summary>
    public string Einleitung { get; init; } =
        "Gleicht die Gerätekataloge mit dem Katalogpaket der Auslieferung ab. Eigene Sätze und " +
        "alle Projekte bleiben unberührt; ein Satz, den Sie geändert haben, wird nicht überschrieben.";

    /// <summary><c>KABG_FASSUNG_DB</c></summary>
    public string FassungDatenbank { get; init; } = "Fassung der Datenbank";

    /// <summary><c>KABG_FASSUNG_PAKET</c></summary>
    public string FassungPaket { get; init; } = "Fassung des Pakets";

    /// <summary><c>KABG_PAKETORT</c></summary>
    public string Paketort { get; init; } = "Katalogpaket";

    /// <summary><c>KABG_BTN_PRUEFEN</c></summary>
    public string Pruefen { get; init; } = "Nur prüfen";

    /// <summary><c>KABG_BTN_ABGLEICHEN</c></summary>
    public string Abgleichen { get; init; } = "Abgleichen…";

    /// <summary><c>KABG_BTN_WIEDERHERSTELLEN</c></summary>
    public string Wiederherstellen { get; init; } = "Auslieferungsstand wiederherstellen…";

    /// <summary><c>KABG_BTN_SCHLIESSEN</c></summary>
    public string Schliessen { get; init; } = "Schließen";

    /// <summary><c>KABG_SPALTE_KATALOG</c></summary>
    public string SpalteKatalog { get; init; } = "Katalog";

    /// <summary><c>KABG_SPALTE_SATZ</c></summary>
    public string SpalteSatz { get; init; } = "Satz";

    /// <summary><c>KABG_SPALTE_AKTION</c></summary>
    public string SpalteAktion { get; init; } = "Aktion";

    /// <summary><c>KABG_SPALTE_HINWEIS</c></summary>
    public string SpalteHinweis { get; init; } = "Hinweis";

    /// <summary><c>KABG_SPALTE_HANDLUNG</c></summary>
    public string SpalteHandlung { get; init; } = "Handlung";

    /// <summary><c>KABG_LEER</c></summary>
    public string Leer { get; init; } = "Noch nicht geprüft.";

    /// <summary><c>KABG_KEINE_AENDERUNG</c></summary>
    public string KeineAenderung { get; init; } = "Der Katalog entspricht dem Paket — nichts zu tun.";

    /// <summary><c>KABG_NUR_GEPRUEFT</c></summary>
    public string NurGeprueft { get; init; } = "Nur geprüft — nichts geschrieben.";

    /// <summary><c>KABG_PROTOKOLL</c></summary>
    public string Protokoll { get; init; } = "Protokoll";

    /// <summary><c>KABG_FRAGE_ABGLEICHEN</c></summary>
    public string FrageAbgleichen { get; init; } =
        "Den Katalog jetzt mit dem Paket abgleichen? Vorher wird eine Sicherung der Datenbank angelegt.";

    /// <summary><c>KABG_FRAGE_WIEDERHERSTELLEN</c> — {0} = Satz.</summary>
    public string FrageWiederherstellen { get; init; } =
        "Den Satz „{0}“ auf den Auslieferungsstand zurücksetzen? Ihre Anpassung geht dabei verloren.";

    /// <summary><c>ALLG_BTN_JA</c></summary>
    public string Ja { get; init; } = "Ja";

    /// <summary><c>ALLG_BTN_NEIN</c></summary>
    public string Nein { get; init; } = "Nein";
}
