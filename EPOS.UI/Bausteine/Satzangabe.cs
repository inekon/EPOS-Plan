namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Eine Kurzangabe des gewählten Satzes</b> (UeS2, Anwenderentscheid 10.10.2026: „Im
/// Bereich ‚Projektsatz' kurze Angaben der Anlagendaten und einen Bearbeiten Button anstelle
/// Details."). Die aufgeklappte Detailzeile der <see cref="Zweispaltenauswahl"/> zeigt eine
/// Liste davon als kompaktes Raster — Beschriftung vor Wert, mehrere je Zeile. Der Wert
/// kommt fertig formatiert (Zahlformat der Kultur, mit Einheit) vom Wirt.
/// </summary>
/// <param name="Beschriftung">Kurze Beschriftung ohne Doppelpunkt („Leistung").</param>
/// <param name="Wert">Der formatierte Wert samt Einheit („120,00 kW").</param>
public sealed record Satzangabe(string Beschriftung, string Wert);
