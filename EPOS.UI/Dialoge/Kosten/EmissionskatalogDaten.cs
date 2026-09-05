using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Kosten;

/// <summary>
/// Eine Zeile der Artenliste (iU9-W3.3) — die Anzeigeform von
/// <c>EmissionsartModel</c>. Die Komponente kennt die Fachklasse des Kerns
/// nicht; die Huelle wandelt hin und zurueck.
/// </summary>
/// <param name="Id"><c>Tab_Emissionsart.ID</c> — der Schluessel fuer alle Delegaten.</param>
/// <param name="Kuerzel">CO2, SO2, NOx, …</param>
/// <param name="Name">Klartextname der Art.</param>
/// <param name="Einheit">g/kWh oder mg/kWh.</param>
/// <param name="Gwp">Der Aequivalenzfaktor GWP100.</param>
/// <param name="GwpText">Derselbe Faktor als fertiger Anzeigetext.</param>
/// <param name="AequivalentQuelle">Woher der Faktor stammt (nur im Editor sichtbar).</param>
/// <param name="Ausgewaehlt">Erscheint die Art als Feld im Emissions-Tab?</param>
/// <param name="IstPflicht">CO2: Haken gesetzt und gesperrt, Faktor bleibt 1 (Konzept F1).</param>
/// <param name="IstAuslieferung">Abwaehlbar, aber nicht loeschbar; das Kuerzel ist fest.</param>
public sealed record EmissionsartZeile(
    int Id,
    string Kuerzel,
    string Name,
    string Einheit,
    double Gwp,
    string GwpText,
    string AequivalentQuelle,
    bool Ausgewaehlt,
    bool IstPflicht,
    bool IstAuslieferung);

/// <summary>
/// Eine Zeile der Werteliste (iU9-W3.3) — die Anzeigeform von
/// <c>EmissionswertModel</c>.
/// </summary>
/// <param name="Id"><c>Tab_Emissionswert.ID</c>.</param>
/// <param name="Herkunftstext">Die Quellspalte der Liste.</param>
/// <param name="QuelleText">Der bearbeitbare Bezeichnungstext eines eigenen Wertes.</param>
/// <param name="Wert">Der Zahlenwert; <c>null</c> = kein Wert gepflegt.</param>
/// <param name="WertText">Derselbe Wert als fertiger Anzeigetext.</param>
/// <param name="IstCo2e">Ist der Wert bereits ein CO2-Aequivalent?</param>
/// <param name="IstAktiv">Der geltende Wert — in der Liste fett.</param>
/// <param name="IstTraegerwert">Traegergebunden (sonst: Vorlage fuer alle Traeger).</param>
/// <param name="AenderungErlaubt">Eigener Wert? Nur dann sind Bearbeiten und Loeschen frei
/// (<c>DarfAendern</c>).</param>
public sealed record EmissionswertZeile(
    int Id,
    string Herkunftstext,
    string QuelleText,
    double? Wert,
    string WertText,
    bool IstCo2e,
    bool IstAktiv,
    bool IstTraegerwert,
    bool AenderungErlaubt);

/// <summary>Was der Arteneditor zurueckgibt (iU9-W3.3).</summary>
/// <param name="Id">0 = neue Art, sonst die zu aendernde.</param>
public sealed record EmissionsartEingabe(
    int Id, string Kuerzel, string Name, string Einheit, double Gwp, string AequivalentQuelle);

/// <summary>Was der Werteeditor zurueckgibt (iU9-W3.3).</summary>
/// <param name="Id">0 = neuer Wert, sonst der zu aendernde.</param>
/// <param name="ArtId">Die Emissionsart, zu der der Wert gehoert.</param>
/// <param name="AlsVorlage">Ohne Traegerbindung anlegen — nur beim Anlegen waehlbar.</param>
public sealed record EmissionswertEingabe(
    int Id, int ArtId, string QuelleText, double Wert, bool IstCo2e, bool AlsVorlage);

/// <summary>
/// Das Ergebnis des Emissionskatalogs (iU9-W3.3) — die drei oeffentlichen
/// Eigenschaften der geloeschten Maske als ein Wert.
/// </summary>
/// <param name="UebernommenId">Der im Rueckgabemodus uebernommene Katalogwert;
/// 0 = keiner (frueher <c>Uebernommen</c>).</param>
/// <param name="ArtenGeaendert">Arten wurden angelegt, geaendert, geloescht oder
/// ab-/angewaehlt — der Emissions-Tab laedt dann seine Feldliste neu.</param>
/// <param name="WerteGeaendert">Es wurde ein Traegerwert geschrieben.</param>
/// <param name="ModusCo2e">Der Stand des Modus-Schalters beim Schliessen. Nur der
/// OK-Weg traegt hier eine Aenderung: Der Vorlaeufer schrieb die globale Vorgabe in
/// <c>Beenden</c>, und weder „Abbrechen" noch die Rueckgabe eines Wertes kamen dort
/// vorbei.</param>
/// <param name="Bestaetigt"><c>true</c> = mit OK oder mit „Uebernehmen" geschlossen,
/// <c>false</c> = mit Abbrechen. Die beiden Aenderungsmerker gelten in BEIDEN Faellen:
/// Was geschrieben wurde, ist geschrieben — der Aufrufer las sie schon in der
/// WinForms-Fassung unabhaengig vom <c>DialogResult</c>.</param>
public sealed record EmissionskatalogErgebnis(
    int UebernommenId, bool ArtenGeaendert, bool WerteGeaendert, bool ModusCo2e, bool Bestaetigt);
