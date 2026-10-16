namespace EPOS.UI.Dialoge.Strom;

/// <summary>
/// Die Reihenwahl des Lastgangbilds der Lastspitzenkappung (Welle RZ): der Ladezustand auf der zweiten Achse,
/// Lade- und Entladeleistung und die erreichte Schwelle. Die zwei neuen Reihen stehen ab Werk aus — ohne sie
/// sind Bild und CSV dieselben wie vorher.
/// </summary>
/// <param name="Ladezustand">Der Schalter „Ladezustand im Diagramm zeigen“.</param>
/// <param name="Leistung">Lade- und Entladeleistung [kW] als Linien.</param>
/// <param name="Schwelle">Die erreichte Schwelle [kW] als waagerechte Linie.</param>
public sealed record PeakShavingBildwahl(bool Ladezustand, bool Leistung = false, bool Schwelle = false);
