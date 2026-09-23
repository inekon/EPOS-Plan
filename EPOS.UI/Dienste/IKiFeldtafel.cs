namespace EPOS.UI.Dienste;

/// <summary>
/// Ein Daten-Objekt, dessen Felder DATEN sind und keine Eigenschaften — die zweite
/// Anmeldeart der <see cref="KiMaskenanmeldung"/> (Welle #456).
/// </summary>
/// <remarks>
/// <para><b>Wofür.</b> Die vier Erzeugerverwaltungen (<c>KatalogBrowserDialog</c>) führen
/// ihren Satz als LISTE von Feldern — je Feld des <c>KatalogBrowserProfil</c> eine Zeile mit
/// Schlüssel, Art und Wert als Text; welche Felder es gibt, sagt das Profil zur Laufzeit.
/// Der Dialogkatalog erzeugt seine Feldkarte aus demselben Profil
/// (<c>KatalogBrowserKiSicht.VORLAUF</c>). Eine Sichtklasse mit einer benannten
/// Eigenschaft je Profilfeld — der Weg des Modulkatalogs — wäre eine ZWEITE Feldliste von
/// Hand neben dem Profil, und die liefe ihm davon.</para>
///
/// <para><b>Wie.</b> Löst ein Eigenschaftspfad per Reflection nicht auf und nennt er vor
/// dem Punkt den Typ des Daten-Objekts, und ist dieses eine Feldtafel, dann ist der Teil
/// NACH dem Punkt ihr Schlüssel: Gelesen und gesetzt wird über
/// <see cref="Lesen"/>/<see cref="Setzen"/>. Den Zieltyp des Setzens nennt der Katalog
/// selbst (Zahl → <c>double?</c>, Ganzzahl → <c>int?</c>, Wahrheitswert → <c>bool</c>,
/// sonst Text) — es gibt keine Eigenschaft, die ihn verriete.</para>
///
/// <para><b>Was sie nicht ersetzt.</b> Benannte Eigenschaften gehen weiter vor: Die Wahl
/// des Satzes (<c>Satz</c> mit <c>SatzWahl</c>) bleibt eine gewöhnliche Eigenschaft der
/// Sichtklasse. Den Feldbestand hält nicht die Reflection-Probe, sondern der Wächter
/// „die Verwaltung deklariert genau die Felder ihres Profils".</para>
/// </remarks>
public interface IKiFeldtafel
{
    /// <summary>Der Wert unter diesem Schlüssel im Typ des Katalogfeldes; <c>null</c> = leer.</summary>
    object? Lesen(string schluessel);

    /// <summary>
    /// Schreibt den Wert unter diesem Schlüssel. Ein Feld, das die offene Maske nicht
    /// schreiben lässt, nimmt nichts an; eine Ablehnung mit Grund kommt als Ausnahme.
    /// </summary>
    void Setzen(string schluessel, object? wert);
}
