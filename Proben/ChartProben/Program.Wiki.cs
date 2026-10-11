using System;
using System.IO;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// Die Wiki-Fassungen der Wärme- und Strombilder (Schalter <c>--wiki</c>): ein eigener, physikalisch stimmiger
    /// Zeitreihensatz. Anders als <see cref="SyntheticherSatz"/> (unabhängige Sinusreihen für die Maß- und Hashproben)
    /// deckt hier eine Kaskade in jeder Stunde genau den Bedarf, damit Stapel und Bedarfslinie im Wiki zusammenpassen.
    /// Die Standardproben und ihre Messlatte berührt das nicht.
    /// </summary>
    internal static partial class Program
    {
        /// <summary>
        /// Wärme: Heizung nach Außentemperatur (Heizgrenze 15 °C) plus Brauchwasser im Tagesgang; Deckung Solarthermie,
        /// BHKW in Grundlast (50 kW, Mindestlast 25 kW), Wärmepumpe bis zu ihrer temperaturabhängigen Leistung,
        /// Heizstab in den kältesten Stunden, Spitzenkessel den Rest. Strom: Grundlast plus Antrieb der Wärmepumpe;
        /// Deckung BHKW, Photovoltaik, Netz; der Überschuss der Photovoltaik wird eingespeist.
        /// </summary>
        private static ZeitreihenSatz WikiSatz()
        {
            var bedarf = new double[STUNDEN]; var temp = new double[STUNDEN];
            var solar = new double[STUNDEN]; var bhkw = new double[STUNDEN]; var wp = new double[STUNDEN];
            var stab = new double[STUNDEN]; var kessel = new double[STUNDEN];
            var strom = new double[STUNDEN]; var bhkwStrom = new double[STUNDEN]; var pvGenutzt = new double[STUNDEN];
            var netz = new double[STUNDEN]; var einspeisung = new double[STUNDEN];

            for (int h = 0; h < STUNDEN; h++)
            {
                int stunde = h % 24;
                double jahr = 2.0 * Math.PI * (h / (double)STUNDEN - 0.04);
                double t = 9.0 - 10.0 * Math.Cos(jahr) + 4.0 * Math.Sin(2.0 * Math.PI * (stunde - 9) / 24.0);
                temp[h] = t;

                double heizung = Math.Max(0.0, 15.0 - t) * 9.0;
                double zapfung = 30.0 + 15.0 * Math.Exp(-Math.Pow(stunde - 7, 2) / 2.0)
                                      + 12.0 * Math.Exp(-Math.Pow(stunde - 19, 2) / 3.0);
                double q = heizung + zapfung;
                bedarf[h] = q;

                // Einstrahlung: Tagesbogen 6 bis 18 Uhr, im Sommer stärker.
                double tagbogen = stunde > 6 && stunde < 18 ? Math.Sin(Math.PI * (stunde - 6) / 12.0) : 0.0;
                double sommer = 0.25 + 0.75 * (1.0 - Math.Cos(jahr)) / 2.0;
                double sonne = tagbogen * sommer;

                double rest = q;
                solar[h] = Math.Min(rest, 60.0 * sonne); rest -= solar[h];
                bhkw[h] = rest >= 25.0 ? Math.Min(rest, 50.0) : 0.0; rest -= bhkw[h];
                wp[h] = Math.Min(rest, 100.0 + 2.0 * t); rest -= wp[h];
                stab[h] = t < -3.0 ? Math.Min(rest, 10.0) : 0.0; rest -= stab[h];
                kessel[h] = rest;

                double cop = 3.0 + 0.08 * t;
                strom[h] = 70.0 + 25.0 * tagbogen + wp[h] / cop + stab[h];
                bhkwStrom[h] = 0.6 * bhkw[h];
                double pv = 120.0 * sonne;
                pvGenutzt[h] = Math.Min(pv, strom[h] - bhkwStrom[h]);
                einspeisung[h] = pv - pvGenutzt[h];
                netz[h] = strom[h] - bhkwStrom[h] - pvGenutzt[h];
            }

            var z = new ZeitreihenSatz();
            z.Reihen[ZeitreihenSatz.WAERMEBEDARF] = bedarf;
            z.Reihen[ZeitreihenSatz.TEMPERATUR] = temp;
            z.Reihen[ZeitreihenSatz.SOLAR_WAERME] = solar;
            z.Reihen[ZeitreihenSatz.BHKW_WAERME] = bhkw;
            z.Reihen[ZeitreihenSatz.WP_WAERME] = wp;
            z.Reihen[ZeitreihenSatz.HEIZSTAB] = stab;
            z.Reihen[ZeitreihenSatz.KESSEL_WAERME] = kessel;
            z.Reihen[ZeitreihenSatz.STROMBEDARF] = strom;
            z.Reihen[ZeitreihenSatz.BHKW_STROM] = bhkwStrom;
            z.Reihen[ZeitreihenSatz.PV_GENUTZT] = pvGenutzt;
            z.Reihen[ZeitreihenSatz.PV_UEBERSCHUSS] = einspeisung;
            z.Reihen[ZeitreihenSatz.NETZBEZUG] = netz;
            return z;
        }

        /// <summary>Schreibt Jahresverlauf, Jahresdauerlinie und Strombilanz aus dem stimmigen Satz.</summary>
        private static void WikiZeitreihenbilder(string ordner)
        {
            ZeitreihenSatz z = WikiSatz();
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Waerme_Jahresverlauf.png"), ChartRenderer.JahresverlaufWaerme(z));
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Waerme_Dauerlinie.png"), ChartRenderer.DauerlinieWaerme(z));
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Strombilanz_Monate.png"), ChartRenderer.StrombilanzMonate(z));
        }
    }
}
