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

// GanglinienWahl und GanglinienKennzahlen standen bis iU9-W9-E-3 hier. Sie
// gehoeren seither zum Baustein und liegen in
// Bausteine/GanglinienGrafikDaten.cs (Namensraum EPOS.UI.Bausteine): Der
// Dialog "Waermebedarf Extern" braucht dieselben zwei Saetze, und ein
// Waermedialog, der einen Satz aus Dialoge/Strom zieht, waere eine Kante, die
// niemand erklaeren kann.
