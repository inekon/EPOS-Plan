using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Woher der freiliegende Umfang P der Bodenplatte kommt (Rechenweg RP2a).</summary>
    internal enum Erdreichumfangsquelle
    {
        /// <summary>Aus dem Feld der Gebäudezeile (<c>Abmessung_Anschluß_Außenwand_Kellerdecke</c>).</summary>
        Feld,

        /// <summary>Benannter Rückfall: das flächengleiche Quadrat P = 4·√A.</summary>
        Quadrat,

        /// <summary>
        /// Die Vorgabe des Gebäudes (<c>Erdreich_U_Wirksam</c>, E65): U_g der Bodenplatte ist der vorgegebene Wert,
        /// B′ wird nicht gerechnet. Der Umfang dient dann allein der Tiefe der Kellerwände.
        /// </summary>
        Vorgabe,
    }

    /// <summary>
    /// <b>Die Erdreichkennwerte eines Gebäudes</b> (Rechenweg RP2a, DIN EN ISO 13370): das charakteristische
    /// Bodenplattenmaß B′ [m], der freiliegende Umfang P [m] samt Herkunft, die Tiefe z [m] (aus Kellerwänden am
    /// Erdreich, sonst 0), die wirksame Gesamtdicke d_t [m], der Wärmedurchgangskoeffizient U_g der Bodenplatte
    /// samt Erdreich [W/(m²K)], der Erdreichwiderstand R_g [m²K/W], der in Reihe zum Bauteil tritt, und der wirksame
    /// U-Wert 1/(1/U + R_g) — flächengewichtet über die Bodenbauteile am Erdreich.
    /// </summary>
    internal sealed record Erdreichkennwerte(double B_M, double Umfang_M, Erdreichumfangsquelle Quelle, double Tiefe_M,
                                             double Dt_M, double Ug_WM2K, double Rg_M2KW, double UWirksam_WM2K)
    {
        /// <summary>Die Herkunft des Umfangs als Text des Exports (<c>Feld</c>, <c>Quadrat</c>, <c>Vorgabe</c>).</summary>
        internal string QuelleText => Quelle switch
        {
            Erdreichumfangsquelle.Feld => "Feld",
            Erdreichumfangsquelle.Vorgabe => QUELLE_VORGABE,
            _ => "Quadrat",
        };

        /// <summary>Der Exporttext der Vorgabe (<c>Geb[n].Erdreich_Umfangsquelle = Vorgabe</c>).</summary>
        internal const string QUELLE_VORGABE = "Vorgabe";
    }

    /// <summary>
    /// <b>Der Erdreichwiderstand nach DIN EN ISO 13370</b> (Rechenweg RP2a, Vorschlag 4). Für Bauteile am Erdreich
    /// tritt zum Bauteilwiderstand der Widerstand des Erdreichs in Reihe; die Randtemperatur bleibt die
    /// Erdreichtemperatur nach Kusuda (<see cref="GebaeudeFestwerte.ERDREICH_TIEFE_M"/>). Die Formeln:
    /// <list type="bullet">
    /// <item>B′ = A/(0,5·P); d_t = w + λ·(R_si + R_f + R_se) (9.1).</item>
    /// <item>Bodenplatte (9.1): d_t &lt; B′ ⇒ U = 2λ/(πB′ + d_t)·ln(πB′/d_t + 1), sonst U = λ/(0,457·B′ + d_t);
    /// Kellerboden in der Tiefe z (9.3.2): d_t durch d_t + 0,5·z ersetzt.</item>
    /// <item>Kellerwand (9.3.3): d_w = λ·(R_si + R_w + R_se),
    /// U_bw = 2λ/(πz)·(1 + 0,5·d_t/(d_t + z))·ln(z/d_w + 1); für z → 0 der Grenzwert 3λ/(π·d_w).</item>
    /// <item>R_g = max(0, 1/U − (R_si + R_f)) mit R_f = 1/U_Bauteil − R_si: der wirksame U-Wert des Bauteils ist
    /// 1/(1/U_Bauteil + R_g) — nie größer als der eingetragene.</item>
    /// </list>
    /// Ohne Datenbank, ohne Zustand.
    /// </summary>
    internal static class Erdreichwiderstand
    {
        private const double LAMBDA = GebaeudeFestwerte.ERDREICH_LAMBDA_WMK;

        /// <summary>Das charakteristische Bodenplattenmaß B′ = A/(0,5·P) [m].</summary>
        internal static double BStrich(double flaecheM2, double umfangM) => flaecheM2 / (0.5 * umfangM);

        /// <summary>
        /// Der freiliegende Umfang [m]: das Feld, wenn es endlich und nicht kleiner als der Umfang des flächengleichen
        /// Kreises 2·√(π·A) ist (kein Grundriss hat weniger), sonst das flächengleiche Quadrat 4·√A.
        /// </summary>
        internal static double Umfang(double flaecheM2, double feldM, out Erdreichumfangsquelle quelle)
        {
            if (feldM > 0.0 && !double.IsInfinity(feldM) && feldM >= 2.0 * Math.Sqrt(Math.PI * flaecheM2))
            {
                quelle = Erdreichumfangsquelle.Feld;
                return feldM;
            }
            quelle = Erdreichumfangsquelle.Quadrat;
            return 4.0 * Math.Sqrt(flaecheM2);
        }

        /// <summary>Die wirksame Gesamtdicke d_t = w + λ·(R_si + R_f + R_se) [m] der Bodenplatte mit R_f [m²K/W].</summary>
        internal static double Dt(double rf) => GebaeudeFestwerte.ERDREICH_WANDDICKE_M
            + LAMBDA * (GebaeudeFestwerte.R_SI_ABWAERTS + rf + GebaeudeFestwerte.ERDREICH_R_SE_NORM);

        /// <summary>
        /// U der Bodenplatte samt Erdreich [W/(m²K)] (9.1, 9.3.2): <paramref name="tiefeM"/> 0 für die Platte auf
        /// dem Erdreich, sonst der Kellerboden in der Tiefe z. B′ → ∞ ⇒ U → 0.
        /// </summary>
        internal static double BodenU(double bStrich, double dt, double tiefeM = 0.0)
        {
            double d = dt + 0.5 * tiefeM;
            if (double.IsPositiveInfinity(bStrich)) return 0.0;
            if (d < bStrich)
                return 2.0 * LAMBDA / (Math.PI * bStrich + d) * Math.Log(Math.PI * bStrich / d + 1.0);
            return LAMBDA / (GebaeudeFestwerte.ERDREICH_FAKTOR_GEDAEMMT * bStrich + d);
        }

        /// <summary>U der Kellerwand samt Erdreich [W/(m²K)] (9.3.3); z = 0 ⇒ der Grenzwert 3λ/(π·d_w).</summary>
        internal static double KellerwandU(double tiefeM, double dt, double dw)
        {
            if (!(tiefeM > 0.0)) return 3.0 * LAMBDA / (Math.PI * dw);
            return 2.0 * LAMBDA / (Math.PI * tiefeM) * (1.0 + 0.5 * dt / (dt + tiefeM)) * Math.Log(tiefeM / dw + 1.0);
        }

        /// <summary>d_w = λ·(R_si + R_w + R_se) [m] der Kellerwand mit R_w [m²K/W].</summary>
        internal static double Dw(double rw) => LAMBDA * (GebaeudeFestwerte.R_SI_HORIZONTAL + rw + GebaeudeFestwerte.ERDREICH_R_SE_NORM);

        /// <summary>
        /// Der Erdreichwiderstand R_g = max(0, 1/U_norm − (R_si + R_f)) [m²K/W] zu einem Bauteil mit
        /// <paramref name="uBauteil"/> [W/(m²K)]; R_f = 1/U_Bauteil − R_si, also R_si + R_f = 1/U_Bauteil.
        /// </summary>
        internal static double Rg(double uNorm, double uBauteil) => Math.Max(0.0, 1.0 / uNorm - 1.0 / uBauteil);

        /// <summary>Der wirksame U-Wert eines Bauteils mit Erdreichwiderstand: 1/(1/U + R_g).</summary>
        internal static double UWirksam(double uBauteil, double rg) => rg > 0.0 ? 1.0 / (1.0 / uBauteil + rg) : uBauteil;

        /// <summary>Ist der wirksame U-Wert der Bodenplatte vorgegeben (endlich und größer als null)?</summary>
        internal static bool IstVorgabe(double uVorgabe) => uVorgabe > 0.0 && !double.IsInfinity(uVorgabe);

        /// <summary>Ist das Bauteil eine Wand (Neigung zwischen 45° und 135°)? Sonst Boden.</summary>
        internal static bool IstWand(double neigungGrad) => neigungGrad > 45.0 && neigungGrad < 135.0;

        /// <summary>
        /// <b>Die Kennwerte und wirksamen U-Werte eines Bauteilsatzes am Erdreich</b>: <paramref name="flaecheGebaeudeM2"/>
        /// ist die Grundfläche der Gebäudezeile (≤ 0 ⇒ die Bodenbauteile am Erdreich), <paramref name="feldUmfangM"/>
        /// das Feld des Umfangs. Jedes Bauteil am Erdreich bekommt seinen wirksamen U-Wert in <paramref name="uWirksam"/>
        /// (Index wie <paramref name="bauteile"/>); <c>null</c>, wenn keines am Erdreich liegt.
        /// <para><b>Vorgabe (E65):</b> Ist <paramref name="uVorgabe"/> endlich und größer als null, ist es U_g jedes
        /// Bodenbauteils am Erdreich; B′ wird nicht gerechnet (<see cref="Erdreichkennwerte.B_M"/> = NaN), die Quelle
        /// ist <see cref="Erdreichumfangsquelle.Vorgabe"/>. Kellerwände rechnen unverändert nach 9.3.3.</para>
        /// </summary>
        internal static Erdreichkennwerte Bauteilsatz(IReadOnlyList<(double FlaecheM2, double NeigungGrad, double U)> bauteile,
                                                     double flaecheGebaeudeM2, double feldUmfangM, double[] uWirksam,
                                                     double uVorgabe = double.NaN)
        {
            bool vorgabe = IstVorgabe(uVorgabe);
            double aBoden = 0.0, aWand = 0.0, uaBoden = 0.0;
            for (int i = 0; i < bauteile.Count; i++)
            {
                if (IstWand(bauteile[i].NeigungGrad)) aWand += bauteile[i].FlaecheM2;
                else
                {
                    aBoden += bauteile[i].FlaecheM2;
                    uaBoden += bauteile[i].U * bauteile[i].FlaecheM2;
                }
            }
            if (aBoden + aWand <= 0.0) return null;
            double a = flaecheGebaeudeM2 > 0.0 ? flaecheGebaeudeM2 : aBoden > 0.0 ? aBoden : aWand;
            double p = Umfang(a, feldUmfangM, out Erdreichumfangsquelle quelle);
            double b = vorgabe ? double.NaN : BStrich(a, p);
            if (vorgabe) quelle = Erdreichumfangsquelle.Vorgabe;
            double z = aWand > 0.0 ? aWand / p : 0.0;
            // d_t der Wand aus dem flächengewichteten Boden; ohne Boden am Erdreich der Boden wie die Wand selbst.
            double dtBoden = aBoden > 0.0 ? Dt(Math.Max(0.0, aBoden / uaBoden - GebaeudeFestwerte.R_SI_ABWAERTS)) : double.NaN;

            double uaNorm = 0.0, flaecheNorm = 0.0, rgFlaeche = 0.0, uaWirksam = 0.0;
            for (int i = 0; i < bauteile.Count; i++)
            {
                double u = bauteile[i].U;
                double uNorm;
                if (IstWand(bauteile[i].NeigungGrad))
                {
                    double dw = Dw(Math.Max(0.0, 1.0 / u - GebaeudeFestwerte.R_SI_HORIZONTAL));
                    double dt = double.IsNaN(dtBoden) ? GebaeudeFestwerte.ERDREICH_WANDDICKE_M + dw : dtBoden;
                    uNorm = KellerwandU(z, dt, dw);
                }
                else
                {
                    double dt = Dt(Math.Max(0.0, 1.0 / u - GebaeudeFestwerte.R_SI_ABWAERTS));
                    uNorm = vorgabe ? uVorgabe : BodenU(b, dt, z);
                    uaNorm += uNorm * bauteile[i].FlaecheM2;
                    flaecheNorm += bauteile[i].FlaecheM2;
                    rgFlaeche += Rg(uNorm, u) * bauteile[i].FlaecheM2;
                }
                uWirksam[i] = UWirksam(u, Rg(uNorm, u));
                if (!IstWand(bauteile[i].NeigungGrad)) uaWirksam += uWirksam[i] * bauteile[i].FlaecheM2;
            }
            double ug = flaecheNorm > 0.0 ? uaNorm / flaecheNorm : double.NaN;
            double rg = flaecheNorm > 0.0 ? rgFlaeche / flaecheNorm : double.NaN;
            double uw = flaecheNorm > 0.0 ? uaWirksam / flaecheNorm : double.NaN;
            return new Erdreichkennwerte(b, p, quelle, z, dtBoden, ug, rg, uw);
        }
    }
}
