using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Projekt;

/// <summary>
/// Der Loeschauftrag der Mehrfachauswahl (Nutzerauftrag 02.09.2026, mit Merge 5 aus
/// <c>Form_ProjektDelete</c> portiert): die gewaehlten Projekte - Varianten VOR ihren
/// Staemmen - und ob vorher eine Sicherungskopie der Datenbank angelegt werden soll.
/// </summary>
public sealed record ProjektLoeschauftrag(IReadOnlyList<ProjektKopfZeile> Projekte, bool Sicherung);
