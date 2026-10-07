namespace EPOS.UI.Dialoge.Import;

// =====================================================================================
//  Die DTO der Nordrichtung (Abstimmungspapier G5, Abschnitt 7): Vorgabe im Zuordnungsdialog
//  und nachträgliche Ausrichtung im Gebäudedialog. Eingabe ist die Richtung der Planoberseite
//  (+y der Datei) in Grad, 0° = Nord, im Uhrzeigersinn; der Nordwinkel ist (360° − α) mod 360°.
// =====================================================================================

/// <summary>Ein Knopf der Schnellwahl: sprachneutrales Kürzel (N, NO, O, SO, S, SW, W, NW), Anzeigename und Richtung.</summary>
public sealed record GebaeudeNordSchnellwahl(string Kuerzel, string Name, double PlanoberseiteGrad);

/// <summary>
/// Die Nordrichtung des gelesenen Imports für den Zuordnungsdialog (N1–N3).
/// </summary>
/// <param name="DateiPlanoberseiteGrad">Die Richtung der Planoberseite nach der Datei [°]; <c>null</c> = die Datei nennt keine.</param>
/// <param name="DateiText">„Die Datei nennt die Nordrichtung: …“ bzw. „Die Datei nennt keine Nordrichtung.“</param>
/// <param name="PlanoberseiteGrad">Die wirksame Richtung der Planoberseite [°] — Eingabe, Dateiwert oder Annahme 0°.</param>
/// <param name="NordwinkelGrad">Der wirksame Nordwinkel [°]; 0 bei der Annahme.</param>
/// <param name="Herkunft">Sprachneutraler Schlüssel der Herkunft: <c>DATEI</c>, <c>EINGABE</c> oder <c>ANNAHME</c>.</param>
/// <param name="HerkunftText">Die Herkunft als Anzeigetext.</param>
/// <param name="Beschriftung">Die Feldbeschriftung „Planoberseite zeigt nach“.</param>
/// <param name="Schnellwahl">Die acht Himmelsrichtungen.</param>
public sealed record GebaeudeNordrichtungDaten(
    double? DateiPlanoberseiteGrad,
    string DateiText,
    double PlanoberseiteGrad,
    double NordwinkelGrad,
    string Herkunft,
    string HerkunftText,
    string Beschriftung,
    IReadOnlyList<GebaeudeNordSchnellwahl> Schnellwahl);

/// <summary>
/// Die gespeicherte Ausrichtung eines Gebäudes für den Gebäudedialog, Abschnitt „Ausrichtung“ (N5/N6).
/// </summary>
/// <param name="Aenderbar">Hat das Gebäude eine Importquelle? Ohne sie ist die Ausrichtung nicht änderbar.</param>
/// <param name="Hinweis">Ohne Quelle der Grund; sonst leer.</param>
/// <param name="PlanoberseiteGrad">Die gespeicherte Richtung der Planoberseite [°]; 0 bei der Annahme.</param>
/// <param name="NordwinkelGrad">Der gespeicherte Nordwinkel [°]; <c>null</c> = keiner (Annahme).</param>
/// <param name="Herkunft">Sprachneutraler Schlüssel der Herkunft: <c>DATEI</c>, <c>EINGABE</c> oder <c>ANNAHME</c>.</param>
/// <param name="HerkunftText">Die Herkunft als Anzeigetext.</param>
/// <param name="Titel">„Ausrichtung“.</param>
/// <param name="Beschriftung">„Planoberseite zeigt nach“.</param>
/// <param name="Schnellwahl">Die acht Himmelsrichtungen.</param>
public sealed record GebaeudeAusrichtungDaten(
    bool Aenderbar,
    string Hinweis,
    double PlanoberseiteGrad,
    double? NordwinkelGrad,
    string Herkunft,
    string HerkunftText,
    string Titel,
    string Beschriftung,
    IReadOnlyList<GebaeudeNordSchnellwahl> Schnellwahl);

/// <summary>Das Ergebnis von „Ausrichtung ändern“: Erfolg, Meldung, Zahl der gedrehten Bauteile und die neue Richtung.</summary>
public sealed record GebaeudeAusrichtungErgebnis(bool Ok, string Meldung, int GedrehteBauteile, double PlanoberseiteGrad);
