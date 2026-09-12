namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Welche Ganglinie gerade markiert ist</b> (iU9-W12, Anwenderwunsch
/// <b>W12‑E‑2</b> vom 05.09.2026) — die Frage, die der Dialog seiner Huelle stellt,
/// bevor sie Zahlen und Bild liefert.
///
/// <para>Der Dialog fuehrt ZWEI Listen mit verschiedenen Schluesseln: rechts den
/// Katalog (Schluessel ist der <c>Bezeichner</c>), links die Projektzuordnungen
/// (Schluessel ist <c>GanglinieId</c>, die Id der PROJEKTKOPIE). Statt zwei
/// Delegaten fuer denselben Zweck sagt dieser Satz, welche der beiden Quellen
/// gemeint ist — die Huelle entscheidet dann, aus welchen Tabellen sie liest.</para>
///
/// <para><b>Seit iU9‑W9‑E‑3 gilt er fuer beide Gewerke</b> — Stromganglinie und
/// externer Waermebedarf fuehren dieselben zwei Listen mit derselben Frage;
/// welche Tabellen dahinterstehen, sagt <c>GanglinienQuelle</c> im Kern.</para>
/// </summary>
/// <param name="AusKatalog"><c>true</c> = rechte Spalte (Katalog), <c>false</c> = linke (Projekt).</param>
/// <param name="GanglinieId">
/// Bei einer Projektzeile die Id der Projektkopie; <c>0</c>, solange die Zuordnung
/// noch nicht gespeichert ist — dann gilt der <paramref name="Bezeichner"/>.
/// </param>
/// <param name="Bezeichner">Der Name; bei einem Katalogsatz zugleich sein Schluessel.</param>
public sealed record GanglinienWahl(bool AusKatalog, int GanglinieId, string Bezeichner);

/// <summary>
/// <b>Die drei Kennzahlen einer Ganglinie</b> (W12‑E‑2, seit W9‑E‑3 auch fuer den
/// Waermebedarf): Jahresarbeit, Spitze, Vollbenutzungsstunden.
///
/// <para>Gerechnet wird im Kern (<c>GanglinienAuswertungCtrl</c>) — diese
/// Bibliothek bekommt fertige Zahlen. Die Jahresarbeit steht in MWh; welche Einheit
/// der Dialog daraus macht, entscheidet die Einheitenwahl (Anwenderentscheid
/// <b>W8‑O‑5</b>). Spitze und Stundenzahl folgen ihr nicht: eine Leistung bleibt kW,
/// Stunden bleiben Stunden.</para>
/// </summary>
/// <param name="JahresarbeitMwh">Die Jahresarbeit in <b>MWh</b>.</param>
/// <param name="SpitzeKw">
/// Die hoechste Stundenleistung in <b>kW</b> — zugleich die 100 %-Linie des Bildes.
/// </param>
/// <param name="VollbenutzungsstundenH">
/// Jahresarbeit durch Spitze [h/a]; <c>null</c>, wenn es keine Spitze gibt.
/// </param>
public sealed record GanglinienKennzahlen(double JahresarbeitMwh, double SpitzeKw,
                                          double? VollbenutzungsstundenH);
