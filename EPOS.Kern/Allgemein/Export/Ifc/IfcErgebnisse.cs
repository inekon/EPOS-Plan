using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Rechenergebnisse eines Objekts für <c>EPOS_Ergebnis</c> (Datenaustauschkonzept 6.4) — alles
    /// optional; was fehlt, wird nicht geschrieben.
    /// </summary>
    /// <param name="HeizwaermebedarfKwh">Heizwärmebedarf im Jahr [kWh].</param>
    /// <param name="HeizlastW">Heizlast (Spitze) [W].</param>
    /// <param name="RaumtemperaturMittelC">Mittlere Raumtemperatur [°C] (nur Räume).</param>
    /// <param name="RaumtemperaturMaxC">Höchste Raumtemperatur [°C] (nur Räume).</param>
    internal sealed record IfcErgebnis(double? HeizwaermebedarfKwh, double? HeizlastW,
                                       double? RaumtemperaturMittelC = null, double? RaumtemperaturMaxC = null);

    /// <summary>
    /// <b>Die Ergebnisse eines Rechenlaufs für den IFC-Export</b> (Stufe G7c). Das
    /// <see cref="GebaeudeAbbild"/> trägt keine Rechenergebnisse; der IFC-Schreiber bekommt sie deshalb
    /// gesondert (<see cref="IfcSchreiber(IfcErgebnisse)"/>). Ohne sie entfallen <c>EPOS_Ergebnis</c>,
    /// <c>Rechenzeitpunkt</c> und <c>Wetterdatensatz</c> benannt (Meldung <c>GEXP_PROT_IFC_OHNE_ERGEBNIS</c>) —
    /// erfunden wird nichts.
    /// </summary>
    internal sealed class IfcErgebnisse
    {
        /// <summary>Die Ergebnisse je Kennung des Abbilds (Gebäude- oder Raumkennung).</summary>
        internal Dictionary<string, IfcErgebnis> JeKennung { get; } = new Dictionary<string, IfcErgebnis>(StringComparer.Ordinal);

        /// <summary>Der Zeitpunkt des Rechenlaufs; <c>null</c> = unbekannt.</summary>
        internal DateTime? Rechenzeitpunkt { get; set; }

        /// <summary>Der Wetterdatensatz des Laufs (die PVGIS-TMY-Reihe nach E5); <c>null</c> = unbekannt.</summary>
        internal string Wetterdatensatz { get; set; }
    }
}
