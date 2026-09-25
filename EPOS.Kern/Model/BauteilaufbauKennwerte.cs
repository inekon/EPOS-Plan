using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kennwerte eines Bauteilaufbaus für die Anzeige</b> (Gebäudesimulation G3,
    /// Mehrzonenkonzept 3.2–3.4 und 5.1) — der Summenfuß des Schichtenrasters und die Spalten U,
    /// R und C der Aufbauliste. Gerechnet in <see cref="BauteilaufbauCtrl.Kennwerte"/> über den
    /// Bauteilweg (<c>Bauteilreduktion</c>); die Oberfläche rechnet nicht.
    ///
    /// <para><b>Jede Größe trägt ihre Einheit im Namen.</b> Widerstände in m²K/W, der
    /// U-Wert in W/(m²K), die Kapazitäten in kJ/(m²K) (flächenbezogen), die Bezugsperiode in
    /// Tagen. <c>null</c> heißt „nicht bestimmbar"; warum, sagt <see cref="Grund"/> bzw.
    /// <see cref="PeriodeGrund"/>.</para>
    ///
    /// <para><b>Die Annahme des Katalogs.</b> Ein Aufbau des Katalogs kennt weder Neigung noch
    /// Randbedingung seines späteren Bauteils. Die Übergangswiderstände folgen deshalb der
    /// Vorgabeneigung seiner Bauteilart (Wand 90°, Dach und Decke 0°, Bodenplatte 180°;
    /// <see cref="GebaeudeZonenCtrl.NeigungVorgabe"/>) und der Außenluft; die Anzeige nennt beide
    /// in ihrer Herleitungszeile.</para>
    /// </summary>
    public sealed record BauteilaufbauKennwerte
    {
        /// <summary>Die angesetzte Neigung β [°] — aus der Bauteilart.</summary>
        public double NeigungGrad { get; init; } = 90.0;

        /// <summary>Innerer Wärmeübergangswiderstand R_si [m²K/W] (DIN EN ISO 6946, Tabelle 7).</summary>
        public double RSi_M2KW { get; init; }

        /// <summary>Äußerer Wärmeübergangswiderstand R_se [m²K/W] — an Außenluft.</summary>
        public double RSe_M2KW { get; init; }

        /// <summary>Wärmedurchlasswiderstand je Schicht [m²K/W], in Schichtreihenfolge; <c>null</c> = nicht bestimmbar.</summary>
        public IReadOnlyList<double?> RJeSchicht_M2KW { get; init; } = Array.Empty<double?>();

        /// <summary>Wärmedurchlasswiderstand des Aufbaus R = Σ d/λ einschließlich ruhender Luftschichten [m²K/W].</summary>
        public double? R_M2KW { get; init; }

        /// <summary>Wärmedurchgangskoeffizient U = 1/(R_si + R + R_se) [W/(m²K)].</summary>
        public double? U_WM2K { get; init; }

        /// <summary>Flächenbezogene Wärmekapazität Σ ρ·c_p·d [kJ/(m²K)].</summary>
        public double? Kapazitaet_KJM2K { get; init; }

        /// <summary>Wirksame (raumseitige) Kapazität C₁ je m² bei der Bezugsperiode [kJ/(m²K)] — VDI 6007 Gl. (12)–(17).</summary>
        public double? KapazitaetWirksam_KJM2K { get; init; }

        /// <summary>Die Bezugsperiode T_BT [d] nach Gl. (10a)–(10d): 7 oder 2.</summary>
        public double? Bezugsperiode_D { get; init; }

        /// <summary>R₁;rel = R₁(2 d)/R₁(7 d) [–] — der Beleg der Wahl.</summary>
        public double? R1Rel { get; init; }

        /// <summary>C₁;rel = C₁(2 d)/C₁(7 d) [–] — der Beleg der Wahl.</summary>
        public double? C1Rel { get; init; }

        /// <summary>
        /// Warum R, U und C nicht stehen — die erste fehlende oder ungültige Angabe, übersetzt;
        /// leer, wenn sie stehen.
        /// </summary>
        public string Grund { get; init; } = "";

        /// <summary>Warum die Bezugsperiode nicht bestimmbar ist (etwa ein Aufbau ohne Speichermasse); leer, wenn sie steht.</summary>
        public string PeriodeGrund { get; init; } = "";

        /// <summary>Stehen R, U und C?</summary>
        public bool Gerechnet => U_WM2K.HasValue;
    }
}
