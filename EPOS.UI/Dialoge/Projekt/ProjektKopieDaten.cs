using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Projekt;

/// <summary>
/// Der Stand eines laufenden Duplizierlaufs (iU9-W15a.4) — der Datentraeger von
/// <c>ProjektDuplizierenCtrl.Fortschritt</c> in einer Form, die <c>EPOS.UI</c>
/// unmittelbar zeigen kann.
///
/// <para>Der Vorlaeufer setzte daraus den Satz „Kopiere Tabelle {0}/{1}: {2}"
/// zusammen — hartkodiert deutsch. Hier reisen die drei Werte einzeln, den Satzbau
/// gibt die Huelle als Textschluessel mit.</para>
/// </summary>
/// <param name="Aktuell">Zahl der bereits kopierten Tabellen.</param>
/// <param name="Gesamt">Zahl der zu kopierenden Tabellen; 0 = noch unbekannt.</param>
/// <param name="Tabelle">Die gerade laufende Tabelle; leer = Fertigstellen.</param>
public readonly record struct KopierStand(int Aktuell, int Gesamt, string Tabelle);

/// <summary>
/// Ergebnis von <c>ProjektDuplizierenCtrl.VerwaltungsfelderSetzen</c> in der Form,
/// die der Dialog braucht: der Befund und der Ausnahmetext (iU9-W15a.4).
/// </summary>
/// <param name="Befund">Wie es ausgegangen ist.</param>
/// <param name="Fehlertext">Der Ausnahmetext bei <see cref="VerwaltungsfelderBefund.Fehler"/>; sonst leer.</param>
public readonly record struct VerwaltungsfelderErgebnis(VerwaltungsfelderBefund Befund, string Fehlertext);
