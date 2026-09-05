using SpeicherEngine;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Strom;

/// <summary>
/// Eine Zeile des Stromganglinien-KATALOGS (<c>Tab_Stromganglinie_STAMM</c>).
///
/// <para>Die Komponente sieht das Modell des Kerns nicht: <c>StromganglinieModel</c>
/// ist dort <c>internal</c>. Sie bekommt deshalb diesen Satz — Name, Raster und das
/// Auslieferungskennzeichen, mehr braucht die Liste nicht.</para>
/// </summary>
/// <param name="Bezeichner">Der Name; er ist zugleich der Schluessel des Katalogs.</param>
/// <param name="Zeitinterval">Intervalle je Stunde: 1 = Stunde, 4 = Viertelstunde.</param>
/// <param name="NurLesen">Auslieferungssatz — er darf nicht geloescht werden.</param>
public sealed record GanglinienKatalogZeile(string Bezeichner, int Zeitinterval, bool NurLesen);

/// <summary>
/// Eine Zeile der PROJEKTauswahl — eine dem Projekt zugeordnete Stromganglinie.
///
/// <para><c>Schluessel</c> ist die Zuordnungs-Id (<c>Z_ProjektStromganglinie.ID</c>),
/// <c>GanglinieId</c> die Id des Katalogeintrags. Beide getrennt zu fuehren ist
/// dieselbe Fachlage wie bei den Erzeugern der Welle 6: Dieselbe Ganglinie darf
/// einem Projekt mehrfach zugeordnet sein — der Vorlaeufer liess das ausdruecklich
/// zu (Befund W12-B5).</para>
/// </summary>
/// <param name="Schluessel">Zuordnungs-Id; bei noch nicht gespeicherten Zeilen eine Nummer ab 100000.</param>
/// <param name="GanglinieId">Id des Katalogeintrags.</param>
/// <param name="Bezeichner">Anzeigename.</param>
public sealed record GanglinienProjektZeile(int Schluessel, int GanglinieId, string Bezeichner);

/// <summary>
/// Das Ergebnis eines Einlesevorgangs, wie es die Verwaltungsmaske anzeigt.
///
/// <para>Die Kette selbst liegt im Kern
/// (<see cref="GanglinienImportAblauf"/>); dieser Satz ist nur, was die
/// Oberflaeche daraus macht: eine Meldung mit ihrer Dringlichkeit.</para>
/// </summary>
/// <param name="Ausgang">Wie die Kette ausgegangen ist.</param>
/// <param name="Meldung">Der fertige Text; leer = nichts zu melden.</param>
/// <param name="Stufe">Dringlichkeit der Meldung.</param>
public sealed record GanglinienImportAnzeige(ImportAusgang Ausgang, string Meldung, PruefStufe Stufe);

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
/// </summary>
/// <param name="AusKatalog"><c>true</c> = rechte Spalte (Katalog), <c>false</c> = linke (Projekt).</param>
/// <param name="GanglinieId">
/// Bei einer Projektzeile die Id der Projektkopie; <c>0</c>, solange die Zuordnung
/// noch nicht gespeichert ist — dann gilt der <paramref name="Bezeichner"/>.
/// </param>
/// <param name="Bezeichner">Der Name; bei einem Katalogsatz zugleich sein Schluessel.</param>
public sealed record GanglinienWahl(bool AusKatalog, int GanglinieId, string Bezeichner);

/// <summary>
/// <b>Die drei Kennzahlen einer Stromganglinie</b> (W12‑E‑2): Jahresarbeit, Spitze,
/// Vollbenutzungsstunden.
///
/// <para>Gerechnet wird im Kern (<c>StromganglinieAuswertungCtrl</c>) — diese
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
