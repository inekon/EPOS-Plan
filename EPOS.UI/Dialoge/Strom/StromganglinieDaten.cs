using SpeicherEngine;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Strom;

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

// GanglinienWahl und GanglinienKennzahlen standen bis iU9-W9-E-3 hier. Sie
// gehoeren seither zum Baustein und liegen in
// Bausteine/GanglinienGrafikDaten.cs (Namensraum EPOS.UI.Bausteine): Der
// Dialog "Waermebedarf Extern" braucht dieselben zwei Saetze, und ein
// Waermedialog, der einen Satz aus Dialoge/Strom zieht, waere eine Kante, die
// niemand erklaeren kann.
