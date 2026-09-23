using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Bauteil der Gebäudehülle in der U·A-Tabelle des Gebäudedialogs
    /// (Umsetzungskonzept Gebäudesimulation 2.5).
    /// </summary>
    public enum Huellbauteil
    {
        /// <summary>Außenwand — U [W/(m²K)] und A [m²].</summary>
        Aussenwand,

        /// <summary>Fenster — U und die Summe der vier Orientierungen.</summary>
        Fenster,

        /// <summary>Dach.</summary>
        Dach,

        /// <summary>Bodenplatte (Grundfläche) — die einzige Zeile mit Randbedingung.</summary>
        Bodenplatte,

        /// <summary>Sonstige Flächen.</summary>
        Sonstiges,

        /// <summary>Wärmebrücke Fenster–Wand — ψ [W/(mK)] und L [m].</summary>
        WaermebrueckeFensterWand,

        /// <summary>Wärmebrücke Außenwand–Keller.</summary>
        WaermebrueckeAussenwandKeller,

        /// <summary>Wärmebrücke Wand–Dach.</summary>
        WaermebrueckeWandDach
    }

    /// <summary>
    /// Eine Zeile der U·A-Tabelle: der Kennwert (U bzw. ψ), die Größe (A bzw. L) und ihr
    /// Produkt [W/K]. Ein leerer Kennwert oder eine leere Größe zählt als 0.
    /// </summary>
    /// <param name="Bauteil">Das Bauteil.</param>
    /// <param name="Kennwert">U [W/(m²K)] bzw. ψ [W/(mK)]; <c>null</c> = leer.</param>
    /// <param name="Groesse">A [m²] bzw. L [m]; <c>null</c> = leer.</param>
    public sealed record Huellzeile(Huellbauteil Bauteil, double? Kennwert, double? Groesse)
    {
        /// <summary>Ist die Zeile eine Wärmebrücke (ψ·L statt U·A)?</summary>
        public bool IstWaermebruecke => Bauteil >= Huellbauteil.WaermebrueckeFensterWand;

        /// <summary>U·A bzw. ψ·L [W/K].</summary>
        public double LeitwertWK => (Kennwert ?? 0.0) * (Groesse ?? 0.0);
    }

    /// <summary>
    /// <b>Die Wärmeleitwerte der Gebäudehülle</b> (Stufe G1; Umsetzungskonzept
    /// Gebäudesimulation 2.5) — die acht Zeilen der U·A-Tabelle und ihre Summen H_T, H_ve,
    /// H_ges sowie der gewichtete Wert des Tagesbilanz-Wegs.
    ///
    /// <para><b>Eine Wahrheit für Dialog und Prüfung.</b> Die Rechnung steht hier, der Dialog
    /// zeigt sie nur an — dieselbe Lage wie bei <see cref="Gebaeudebauweise"/> und
    /// <see cref="Ferienzeit"/>: eine reine, plattformfreie Hilfsklasse ohne Datenbank.</para>
    ///
    /// <para><b>Ungewichtet und gewichtet.</b> Das Stundenmodell rechnet H_T ohne verdeckte
    /// Faktoren (E2): Σ U·A + Σ ψ·L. Der Tagesbilanz-Weg wichtet Außenwand und Wärmebrücken mit
    /// 0,83, das Dach mit 0,95 und die Bodenplatte mit 0,45 — dieselben Gewichte wie
    /// <c>TagesbilanzPhysik.SpezWaermeverlusteC</c>; die Kernprobe hält beide auf 1e-12.
    /// H_ve rechnet mit 0,34 Wh/(m³K) wie das Stundenmodell.</para>
    /// </summary>
    public static class Gebaeudehuellbilanz
    {
        /// <summary>Volumenbezogene Wärmekapazität der Luft [Wh/(m³K)] — wie das Stundenmodell.</summary>
        public const double C_RHO_LUFT = GebaeudeFestwerte.C_RHO_LUFT;

        /// <summary>Gewicht der Außenwand und der Wärmebrücken im Tagesbilanz-Weg.</summary>
        public const double GEWICHT_WAND = 0.83;

        /// <summary>Gewicht des Dachs im Tagesbilanz-Weg.</summary>
        public const double GEWICHT_DACH = 0.95;

        /// <summary>Gewicht der Bodenplatte im Tagesbilanz-Weg.</summary>
        public const double GEWICHT_BODEN = 0.45;

        /// <summary>Kleinster zulässiger U-Wert [W/(m²K)] (Konzept 4.8).</summary>
        public const double U_MIN = GebaeudeFestwerte.U_MIN;

        /// <summary>Größter zulässiger U-Wert [W/(m²K)] (Konzept 4.8).</summary>
        public const double U_MAX = GebaeudeFestwerte.U_MAX;

        /// <summary>
        /// Größtes mittleres U der opaken Bauteile [W/(m²K)], bei dem der Restwiderstand der
        /// Außenwände noch positiv ist (Konzept 4.8: R_Rest,AW &gt; 0).
        /// </summary>
        public const double U_OPAK_MITTEL_MAX = 4.17;

        /// <summary>Untergrenze der Bauweise je m² Nutzfläche [Wh/(m²K)] (Konzept 4.8).</summary>
        public const double BAUWEISE_JE_M2_MIN = GebaeudeFestwerte.BAUWEISE_JE_M2_MIN;

        /// <summary>Obergrenze der Bauweise je m² Nutzfläche [Wh/(m²K)] (Konzept 4.8).</summary>
        public const double BAUWEISE_JE_M2_MAX = GebaeudeFestwerte.BAUWEISE_JE_M2_MAX;

        /// <summary>Die Transmissionszeilen in der Reihenfolge der Tabelle.</summary>
        public static readonly IReadOnlyList<Huellbauteil> REIHENFOLGE = new[]
        {
            Huellbauteil.Aussenwand, Huellbauteil.Fenster, Huellbauteil.Dach,
            Huellbauteil.Bodenplatte, Huellbauteil.Sonstiges,
            Huellbauteil.WaermebrueckeFensterWand, Huellbauteil.WaermebrueckeAussenwandKeller,
            Huellbauteil.WaermebrueckeWandDach
        };

        /// <summary>H_T = Σ U·A + Σ ψ·L [W/K], ungewichtet.</summary>
        public static double TransmissionWK(IEnumerable<Huellzeile> zeilen)
        {
            double summe = 0.0;
            foreach (Huellzeile z in zeilen) summe += z.LeitwertWK;
            return summe;
        }

        /// <summary>
        /// H_T gewichtet nach dem Tagesbilanz-Weg [W/K]:
        /// 0,83·U_w·A_w + U_f·A_f + 0,95·U_d·A_d + 0,45·U_g·A_g + U_s·A_s + 0,83·Σψ·L.
        /// Summiert in derselben Reihenfolge wie <c>SpezWaermeverlusteC</c>.
        /// </summary>
        public static double TransmissionGewichtetWK(IEnumerable<Huellzeile> zeilen)
        {
            double aw = 0, f = 0, d = 0, g = 0, s = 0, wb1 = 0, wb2 = 0, wb3 = 0;
            foreach (Huellzeile z in zeilen)
            {
                switch (z.Bauteil)
                {
                    case Huellbauteil.Aussenwand: aw = z.LeitwertWK; break;
                    case Huellbauteil.Fenster: f = z.LeitwertWK; break;
                    case Huellbauteil.Dach: d = z.LeitwertWK; break;
                    case Huellbauteil.Bodenplatte: g = z.LeitwertWK; break;
                    case Huellbauteil.Sonstiges: s = z.LeitwertWK; break;
                    case Huellbauteil.WaermebrueckeFensterWand: wb1 = z.LeitwertWK; break;
                    case Huellbauteil.WaermebrueckeWandDach: wb2 = z.LeitwertWK; break;
                    case Huellbauteil.WaermebrueckeAussenwandKeller: wb3 = z.LeitwertWK; break;
                }
            }

            double transmission = GEWICHT_WAND * aw + f + GEWICHT_DACH * d + GEWICHT_BODEN * g + s;
            double bruecken = (wb1 + wb2 + wb3) * GEWICHT_WAND;
            return transmission + bruecken;
        }

        /// <summary>
        /// H_ve = Luftwechselrate · Nutzfläche · Raumhöhe · 0,34 Wh/(m³K) [W/K]. Leere Werte
        /// zählen als 0.
        /// </summary>
        public static double LueftungWK(double? luftwechselrate, double? nutzflaeche, double? raumhoehe)
            => (luftwechselrate ?? 0.0) * (nutzflaeche ?? 0.0) * (raumhoehe ?? 0.0) * C_RHO_LUFT;

        /// <summary>
        /// Das mittlere U der opaken Bauteile (Außenwand, Dach, Bodenplatte, Sonstiges)
        /// [W/(m²K)], flächengewichtet; <c>null</c>, wenn ihre Fläche 0 ist.
        /// </summary>
        public static double? MittleresUOpak(IEnumerable<Huellzeile> zeilen)
        {
            double ua = 0.0, a = 0.0;
            foreach (Huellzeile z in zeilen)
            {
                if (z.IstWaermebruecke || z.Bauteil == Huellbauteil.Fenster) continue;
                ua += z.LeitwertWK;
                a += z.Groesse ?? 0.0;
            }
            return a > 0.0 ? ua / a : (double?)null;
        }

        /// <summary>
        /// Die acht Zeilen aus einem Katalog- oder Projektsatz. Die Fensterfläche ist die
        /// Summe der vier Orientierungen (Süd, Ost, West, Nord); fehlen Ost und West, gilt
        /// das Bestandsfeld Ost + West.
        /// </summary>
        public static IReadOnlyList<Huellzeile> Zeilen(
            double? uAussenwand, double? aAussenwand, double? uFenster, double? aFenster,
            double? uDach, double? aDach, double? uBoden, double? aBoden,
            double? uSonstiges, double? aSonstiges,
            double? psiFensterWand, double? lFensterWand,
            double? psiAussenwandKeller, double? lAussenwandKeller,
            double? psiWandDach, double? lWandDach)
        {
            return new[]
            {
                new Huellzeile(Huellbauteil.Aussenwand, uAussenwand, aAussenwand),
                new Huellzeile(Huellbauteil.Fenster, uFenster, aFenster),
                new Huellzeile(Huellbauteil.Dach, uDach, aDach),
                new Huellzeile(Huellbauteil.Bodenplatte, uBoden, aBoden),
                new Huellzeile(Huellbauteil.Sonstiges, uSonstiges, aSonstiges),
                new Huellzeile(Huellbauteil.WaermebrueckeFensterWand, psiFensterWand, lFensterWand),
                new Huellzeile(Huellbauteil.WaermebrueckeAussenwandKeller, psiAussenwandKeller, lAussenwandKeller),
                new Huellzeile(Huellbauteil.WaermebrueckeWandDach, psiWandDach, lWandDach)
            };
        }

        /// <summary>
        /// Die Fensterfläche Ost + West: Summe der beiden getrennten Felder, und wenn beide
        /// leer sind, das Bestandsfeld. <c>null</c>, wenn genau eines der beiden steht — dann
        /// ist die Eingabe unvollständig (der Kern setzt für ein leeres Feld die Hälfte des
        /// Bestandsfelds ein, <c>GebaeudeVorbereitung.FensterflaechenOstWest</c>).
        /// </summary>
        public static double? FensterOstWest(double? ost, double? west, double? bestandOstWest)
        {
            if (ost.HasValue && west.HasValue) return ost.Value + west.Value;
            if (!ost.HasValue && !west.HasValue) return bestandOstWest;
            return null;
        }

        /// <summary>
        /// H_ges eines Katalog- bzw. Projektsatzes aus dem Kern-Modell [W/K] — die leise
        /// Kennzahl des Gebäudedialogs. Bezugsfläche ist die Nutzfläche.
        /// </summary>
        internal static double GesamtWK(GebaeudeModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            IReadOnlyList<Huellzeile> zeilen = Zeilen(
                m.k_Wert_Außenwand, m.Flaeche_Außenwand, m.k_Wert_Fenster, m.gesamte_Fensterflaeche,
                m.k_Wert_Dachflaeche, m.Dachflaeche, m.k_Wert_Grundflaeche, m.Grundflaeche,
                m.k_Wert_Sonstiges, m.Sonstige_Flaechen,
                m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand, m.Abmessung_Anschluß_Fenster_Wand,
                m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, m.Abmessung_Anschluß_Außenwand_Kellerdecke,
                m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, m.Abmessung_Anschluß_Wand_Dach);
            return TransmissionWK(zeilen) + LueftungWK(m.Luftwechselrate, m.Nutzflaeche, m.Raumhoehe);
        }

        /// <summary>Dasselbe für ein Projektgebäude, wie der Lauf es liest.</summary>
        internal static double GesamtWK(ProjektGebaeudeModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            IReadOnlyList<Huellzeile> zeilen = Zeilen(
                m.k_Wert_Außenwand, m.Flaeche_Außenwand, m.k_Wert_Fenster, m.gesamte_Fensterflaeche,
                m.k_Wert_Dachflaeche, m.Dachflaeche, m.k_Wert_Grundflaeche, m.Grundflaeche,
                m.k_Wert_Sonstiges, m.Sonstige_Flaechen,
                m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand, m.Abmessung_Anschluß_Fenster_Wand,
                m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, m.Abmessung_Anschluß_Außenwand_Kellerdecke,
                m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, m.Abmessung_Anschluß_Wand_Dach);
            return TransmissionWK(zeilen) + LueftungWK(m.Luftwechselrate, m.Nutzflaeche, m.Raumhoehe);
        }
    }
}
