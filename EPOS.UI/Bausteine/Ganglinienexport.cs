using System;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>CSV am Diagramm, ein Handgriff:</b> Ein Wirt reicht diese Naht als <c>CascadingValue</c> um
/// seine Diagramme, und jeder <see cref="DiagrammSvg"/> darunter, dessen Modell eine Zeitreihe
/// trägt, zeigt in seiner Zoomleiste den Knopf „CSV…“. Der Klick gibt das gezeigte
/// <see cref="Zeichenmodell"/> samt Diagrammtitel an die Plattform; die Hülle schreibt daraus über
/// den Kern (<c>ZeitreihenCsv</c>, Dateiwahl über <c>Dienste.Datei</c>) die Datei — Stunde,
/// Viertelstunde, Tag, Woche, Monat, Jahr, Tages- oder Wochenstunde als erste Spalte, je Reihe „Name [Einheit]“.
/// </summary>
public sealed class Ganglinienexport
{
    private readonly Func<Zeichenmodell, string, Zeitraster, Task> _speichern;

    /// <param name="speichern">Der Weg der Hülle: Modell und Diagrammtitel; das Raster folgt der Länge der Reihen.</param>
    public Ganglinienexport(Func<Zeichenmodell, string, Task> speichern)
    {
        ArgumentNullException.ThrowIfNull(speichern);
        _speichern = (m, t, _) => speichern(m, t);
    }

    /// <param name="speichern">Der Weg der Hülle: Modell, Diagrammtitel und Raster der ersten Spalte.</param>
    /// <param name="raster">
    /// Das ausdrückliche Raster (Jahr für Betrachtungsjahre, <see cref="Zeitraster.Tagesstunde"/>
    /// für 24, <see cref="Zeitraster.Wochenstunde"/> für 168 Werte); <c>null</c> = aus der Länge.
    /// </param>
    public Ganglinienexport(Func<Zeichenmodell, string, Zeitraster, Task> speichern, Zeitraster? raster = null)
    {
        _speichern = speichern ?? throw new ArgumentNullException(nameof(speichern));
        Raster = raster;
    }

    /// <summary>Das ausdrücklich gesetzte Raster; <c>null</c> = aus der Länge der längsten Reihe.</summary>
    public Zeitraster? Raster { get; }

    /// <summary>Das Raster, unter dem das Modell geschrieben wird.</summary>
    public Zeitraster RasterFuer(Zeichenmodell modell)
        => modell?.Tafel is not null ? Zeitraster.Kalendertag
         : Raster ?? ZeitreihenCsv.RasterAus(ZeitreihenCsv.AusModell(modell));

    /// <summary>Speichert die Reihen des Modells; der Titel wird Dateistamm.</summary>
    public Task Speichern(Zeichenmodell modell, string titel) => _speichern(modell, titel ?? "", RasterFuer(modell));

    /// <summary>
    /// Trägt das Modell eine Zeitreihe? Eine Zeichenfläche mit Stunden- oder Indexachse, oder ein
    /// Säulenbild ohne Zeichenfläche, das seine Reihen im Modell führt — und dazu mindestens eine
    /// Linie oder Fläche mit Werten, oder ein Kalenderteppich mit seiner Tafel (<see cref="Zeitraster.Kalendertag"/>). Punktwolken (x = Wert), Kennlinien und Ringe nicht; eine
    /// Wertachse nur mit dem ausdrücklichen Raster <see cref="Zeitraster.Jahr"/>.
    /// </summary>
    public bool Passt(Zeichenmodell? modell)
    {
        if (modell is null) return false;
        // Ein Kalenderteppich trägt seine Tafel (365 Tage × 24 Stunden) statt einer Reihe.
        if (modell.Tafel is { Werte.Length: > 0 }) return true;
        // Eine Wertachse ist eine Kennlinie — außer die Naht sagt ausdrücklich „Jahr“: Der
        // Kapitalwertverlauf zählt Betrachtungsjahre auf einer Wertachse („a“), gleichabständig
        // und ohne eigene x-Werte.
        bool jahresachse = Raster == Zeitraster.Jahr;
        if (modell.Flaeche is not null && modell.Flaeche.X == Achsenart.Wert && !jahresachse) return false;
        foreach (Datenreihe r in modell.Reihen)
            if (r.Art != Reihenart.Punkte && r.Werte is { Length: > 0 } && (!jahresachse || r.XWerte is null))
                return true;
        return false;
    }
}
