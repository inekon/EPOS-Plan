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
            if (monatswerteC == null || monatswerteC.Length != Zapfkalender.MONATE)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                    "Nicht rechenbar — der Kaltwassergang trägt nicht zwölf Monatswerte.");
            double mitte = zapfC - mittelC;
            if (!(mitte > 0))
                throw new ZapfprofilEingabeException(ZapfEingabefehler.TemperaturUngueltig, zone,
                    "Nicht rechenbar — die Zapftemperatur liegt nicht über dem Kaltwassermittel (Zone „" + zone + "“).");
            var f = new double[Zapfkalender.MONATE];
            for (int m = 0; m < Zapfkalender.MONATE; m++)
            {
                double delta = zapfC - monatswerteC[m];
                if (!(delta > 0))
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.TemperaturUngueltig, zone,
                        "Nicht rechenbar — im Monat " + (m + 1) + " liegt das Kaltwasser nicht unter der Zapftemperatur (Zone „"
                        + zone + "“).");
                f[m] = delta / mitte;
            }
            return f;
        }

        /// <summary>Die Kaltwasserfaktoren einer Zone aus ihren Temperaturen.</summary>
        internal static double[] Monatsfaktoren(Zonentemperaturen t, string zone = "")
        {
            double[] werte = Monatswerte(t.KaltwasserMittelC, t.KaltwasserAmplitudeK, t.KaltwasserMonatMaximum);
            return Monatsfaktoren(t.ZapfC, werte, t.KaltwasserMittelC, zone);
        }
    }
}
