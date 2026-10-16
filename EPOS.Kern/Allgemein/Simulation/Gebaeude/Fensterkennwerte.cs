using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Fensterwerte eines Aufbaus (BK-6, Spalten aus Schritt 221 und <see cref="BauteilkatalogAbschnittSchema"/>):
    /// U_g [W/(m²K)], U_f [W/(m²K)], g-Wert (0…1), Rahmenanteil f = A_f / (A_g + A_f) (0…1) und ψ_g des Glasrands [W/(m·K)];
    /// <c>null</c> = nicht angegeben.
    /// </summary>
    public sealed record Fensteraufbau(double? Ug, double? Uf, double? GWert, double? Rahmenanteil, double? PsiGlasrand)
    {
        /// <summary>Führt der Aufbau einen Fensterwert?</summary>
        public bool Vorhanden => Ug.HasValue || Uf.HasValue || GWert.HasValue || Rahmenanteil.HasValue || PsiGlasrand.HasValue;
    }

    /// <summary>
    /// <b>Fenster im Aufbaukatalog</b> (BK-6): die Fensterwerte eines Aufbaus und U_w nach der Formel von DIN EN ISO 10077-1
    /// U_w = (A_g·U_g + A_f·U_f + l_g·ψ_g) / (A_g + A_f). Mit dem Rahmenanteil f = A_f / A und A = A_g + A_f:
    /// U_w = (1 − f)·U_g + f·U_f + (l_g / A)·ψ_g. Die Glasrandlänge ist eine Näherung über die Bauteilfläche: EINE quadratische
    /// Scheibe der Glasfläche A_g = (1 − f)·A, also l_g ≈ 4·√A_g. Ohne ψ_g oder ohne Fläche entfällt der Randterm. Ohne Datenbank.
    /// </summary>
    public static class Fensterkennwerte
    {
        /// <summary>Die Fensterwerte eines Aufbaus aus seinen Katalogfeldern (Zusatz).</summary>
        public static Fensteraufbau AusAufbau(BauteilaufbauModel m)
        {
            IReadOnlyDictionary<string, object> z = m?.Zusatz ?? new Dictionary<string, object>();
            return new Fensteraufbau(Bauteilkatalog.Zahl(z, SPALTE_UG), Bauteilkatalog.Zahl(z, SPALTE_UF),
                                     Bauteilkatalog.Zahl(z, SPALTE_GWERT), Bauteilkatalog.Zahl(z, SPALTE_RAHMENANTEIL),
                                     Bauteilkatalog.Zahl(z, BauteilkatalogAbschnittSchema.SPALTE_PSI_GLASRAND));
        }

        /// <summary>U_g der Verglasung (Spalte aus Schritt 221).</summary>
        public const string SPALTE_UG = "Ug";
        /// <summary>U_f des Rahmens (Spalte aus Schritt 221).</summary>
        public const string SPALTE_UF = "Uf";
        /// <summary>g-Wert (Spalte aus Schritt 221, Name wie an <c>Tab_Bauteil</c>).</summary>
        public const string SPALTE_GWERT = "g_Wert";
        /// <summary>Rahmenanteil (Spalte aus Schritt 221, Name wie an <c>Tab_Bauteil</c>).</summary>
        public const string SPALTE_RAHMENANTEIL = "Rahmenanteil";

        /// <summary>Die Bauteilarten mit Fensterwerten (Fenster, Vorhangfassade).</summary>
        public static bool IstFensterart(string bauteilart) =>
            bauteilart == DbWerte.BAUTEILART_FENSTER || bauteilart == DbWerte.BAUTEILART_VORHANGFASSADE;

        /// <summary>
        /// <b>U_w</b> [W/(m²K)] — <c>null</c>, wenn U_g fehlt oder ein Rahmenanteil größer null ohne U_f steht. Ohne Rahmenanteil
        /// gilt f = 0. <paramref name="flaeche_M2"/> ist die Fläche des Bauteils (Fensterfläche A); ohne sie oder ohne ψ_g kein
        /// Randterm.
        /// </summary>
        public static double? Uw(Fensteraufbau f, double? flaeche_M2)
        {
            if (f == null || !f.Ug.HasValue) return null;
            double anteil = f.Rahmenanteil ?? 0.0;
            if (anteil < 0 || anteil > 1) return null;
            if (anteil > 0 && !f.Uf.HasValue) return null;
            double u = (1.0 - anteil) * f.Ug.Value + anteil * (f.Uf ?? 0.0);
            if (f.PsiGlasrand is double psi && flaeche_M2 is double a && a > 0)
                u += Glasrandlaenge(a, anteil) * psi / a;
            return u;
        }

        /// <summary>Die genäherte Glasrandlänge l_g ≈ 4·√((1 − f)·A) [m] — eine quadratische Scheibe.</summary>
        public static double Glasrandlaenge(double flaeche_M2, double rahmenanteil) =>
            4.0 * Math.Sqrt(Math.Max(0.0, (1.0 - rahmenanteil) * flaeche_M2));
    }
}
