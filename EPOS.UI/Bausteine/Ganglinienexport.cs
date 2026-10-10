using System;
using System.Threading.Tasks;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>CSV am Diagramm, ein Handgriff:</b> Ein Wirt reicht diese Naht als <c>CascadingValue</c> um
/// seine Diagramme, und jeder <see cref="DiagrammSvg"/> darunter, dessen Modell eine Zeitreihe
/// trägt, zeigt in seiner Zoomleiste den Knopf „CSV…“. Der Klick gibt das gezeigte
/// <see cref="Zeichenmodell"/> samt Diagrammtitel an die Plattform; die Hülle schreibt daraus über
/// den Kern (<c>ZeitreihenCsv</c>, Dateiwahl über <c>Dienste.Datei</c>) die Datei — Stunde,
/// Viertelstunde, Tag, Monat oder Jahr als erste Spalte, je Reihe „Name [Einheit]“.
/// </summary>
public sealed class Ganglinienexport
{
    private readonly Func<Zeichenmodell, string, Task> _speichern;

    /// <param name="speichern">Der Weg der Hülle: Modell und Diagrammtitel.</param>
    public Ganglinienexport(Func<Zeichenmodell, string, Task> speichern)
        => _speichern = speichern ?? throw new ArgumentNullException(nameof(speichern));

    /// <summary>Speichert die Reihen des Modells; der Titel wird Dateistamm.</summary>
    public Task Speichern(Zeichenmodell modell, string titel) => _speichern(modell, titel ?? "");

    /// <summary>
    /// Trägt das Modell eine Zeitreihe? Eine Zeichenfläche mit Stunden- oder Indexachse und
    /// mindestens eine Linie oder Fläche mit Werten — Punktwolken und Ringe nicht.
    /// </summary>
    public bool Passt(Zeichenmodell? modell)
    {
        if (modell?.Flaeche is null || modell.Flaeche.X == Achsenart.Wert) return false;
        foreach (Datenreihe r in modell.Reihen)
            if (r.Art != Reihenart.Punkte && r.Werte is { Length: > 0 }) return true;
        return false;
    }
}
