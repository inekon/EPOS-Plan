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
    /// <param name="KaeltebedarfKwh">Kältebedarf im Jahr [kWh], sensibel, ohne Entfeuchtung (Kühlkonzept 9.2).</param>
    /// <param name="KaeltelastW">Kältelast (Spitze) [W].</param>
    internal sealed record IfcErgebnis(double? HeizwaermebedarfKwh, double? HeizlastW,
                                       double? RaumtemperaturMittelC = null, double? RaumtemperaturMaxC = null,
                                       double? KaeltebedarfKwh = null, double? KaeltelastW = null);

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

        /// <summary>
        /// <b>Die Ergebnisse aus dem Abbild</b> (Stufe G7c, Teil 2): das Gebäudeergebnis
        /// (<see cref="AbbildGebaeude.Ergebnis"/>, dort auch Rechenzeitpunkt und Wetterdatensatz) und die
        /// Ergebnisse der Räume (<see cref="AbbildRaum.Ergebnis"/>) unter ihren Kennungen. Trägt kein Gebäude und
        /// kein Raum ein Ergebnis, ist es <c>null</c> — dann schreibt der Schreiber wie ohne Rechenlauf.
        /// </summary>
        internal static IfcErgebnisse AusAbbild(GebaeudeAbbild abbild)
        {
            if (abbild == null) return null;
            IfcErgebnisse e = null;
            foreach (AbbildGebaeude g in abbild.Gebaeude)
            {
                if (g.Ergebnis != null)
                {
                    e ??= new IfcErgebnisse();
                    e.JeKennung[g.Kennung] = Aus(g.Ergebnis);
                    e.Rechenzeitpunkt ??= g.Ergebnis.Rechenzeitpunkt;
                    e.Wetterdatensatz ??= string.IsNullOrWhiteSpace(g.Ergebnis.Wetterdatensatz) ? null : g.Ergebnis.Wetterdatensatz.Trim();
                }
                foreach (AbbildRaum r in g.Raeume)
                {
                    if (r.Ergebnis == null) continue;
                    e ??= new IfcErgebnisse();
                    e.JeKennung[r.Kennung] = Aus(r.Ergebnis);
                }
            }
            return e;
        }

        private static IfcErgebnis Aus(AbbildErgebnis a)
            => new IfcErgebnis(a.EnergieKWh, a.HeizlastW, a.MitteltemperaturC, null, a.KaeltebedarfKWh, a.KaeltelastW);
    }
}
