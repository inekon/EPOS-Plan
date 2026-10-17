using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Schicht S2 — der Kaltwasser-Jahresgang der Bilanz</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.2, Entscheid K4: fester Jahresgang, Parameter aus Zone bzw.
    /// Parametersatz).
    ///
    /// <code>
    /// θ_KW(m) = Runden₉( θ̄_KW + A · cos(2π · (m − m_max) / 12) ),  m = 1 … 12
    /// f_KW(m) = (θ_Zapf − θ_KW(m)) / (θ_Zapf − θ̄_KW)
    /// </code>
    ///
    /// <para><b>Runden auf neun Stellen</b>: Die zwölf Monatswerte entstehen einmal je Lauf und
    /// sind danach Zahlen ohne Abhängigkeit von der Mathematik-Bibliothek der Plattform
    /// (Konzept 4.2, 4.4). Die Auslegung rechnet unabhängig davon mit θ_KW,Auslegung (Z2).</para>
    /// </summary>
    internal static class Kaltwassergang
    {
        /// <summary>Stellen der Rundung der Monatswerte.</summary>
        internal const int STELLEN = 9;

        /// <summary>Die zwölf Kaltwassertemperaturen [°C] der Monate 1 … 12, auf neun Stellen gerundet.</summary>
        internal static double[] Monatswerte(double mittelC, double amplitudeK, int monatMaximum)
        {
            var werte = new double[Zapfkalender.MONATE];
            for (int m = 1; m <= Zapfkalender.MONATE; m++)
            {
                double x = mittelC + amplitudeK * Math.Cos(2.0 * Math.PI * (m - monatMaximum) / Zapfkalender.MONATE);
                werte[m - 1] = Math.Round(x, STELLEN);
            }
            return werte;
        }

        /// <summary>
        /// Die zwölf Kaltwasserfaktoren <c>f_KW(m)</c>. Eine nicht positive Spreizung —
        /// im Mittel oder in einem Monat — wird benannt abgelehnt.
        /// </summary>
        internal static double[] Monatsfaktoren(double zapfC, double[] monatswerteC, double mittelC, string zone = "")
        {
            ZapfAblehnung befund = Befund(zapfC, monatswerteC, mittelC, zone);
            if (befund != null) throw befund.Ausnahme();
            double mitte = zapfC - mittelC;
            var f = new double[Zapfkalender.MONATE];
            for (int m = 0; m < Zapfkalender.MONATE; m++) f[m] = (zapfC - monatswerteC[m]) / mitte;
            return f;
        }

        /// <summary>
        /// Die Regel der <see cref="Monatsfaktoren(double, double[], double, string)"/> ohne Wurf: <c>null</c>,
        /// wenn die Zapftemperatur über dem Kaltwassermittel und über dem Kaltwasser jedes Monats liegt;
        /// sonst die Ablehnung.
        /// </summary>
        internal static ZapfAblehnung Befund(double zapfC, double[] monatswerteC, double mittelC, string zone = "")
        {
            if (monatswerteC == null || monatswerteC.Length != Zapfkalender.MONATE)
                return new ZapfAblehnung(zone ?? "", ZapfEingabefehler.RasterUngueltig, ZapfSatz.Neu("EINGABE_KALTWASSERGANG_RASTER"));
            if (!(zapfC - mittelC > 0))
                return new ZapfAblehnung(zone ?? "", ZapfEingabefehler.TemperaturUngueltig,
                    ZapfSatz.Neu("EINGABE_ZAPFTEMPERATUR_UNTER_KALTWASSER", zone));
            for (int m = 0; m < Zapfkalender.MONATE; m++)
                if (!(zapfC - monatswerteC[m] > 0))
                    return new ZapfAblehnung(zone ?? "", ZapfEingabefehler.TemperaturUngueltig,
                        ZapfSatz.Neu("EINGABE_KALTWASSER_MONAT_UEBER_ZAPF", m + 1, zone));
            return null;
        }

        /// <summary>Die Regel der Kaltwasserfaktoren einer Zone aus ihren Temperaturen, ohne Wurf.</summary>
        internal static ZapfAblehnung Befund(Zonentemperaturen t, string zone = "")
            => Befund(t.ZapfC, Monatswerte(t.KaltwasserMittelC, t.KaltwasserAmplitudeK, t.KaltwasserMonatMaximum),
                      t.KaltwasserMittelC, zone);

        /// <summary>Die Kaltwasserfaktoren einer Zone aus ihren Temperaturen.</summary>
        internal static double[] Monatsfaktoren(Zonentemperaturen t, string zone = "")
        {
            double[] werte = Monatswerte(t.KaltwasserMittelC, t.KaltwasserAmplitudeK, t.KaltwasserMonatMaximum);
            return Monatsfaktoren(t.ZapfC, werte, t.KaltwasserMittelC, zone);
        }
    }
}
