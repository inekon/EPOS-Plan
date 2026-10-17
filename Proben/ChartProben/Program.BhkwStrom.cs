using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DIE STROMLAST DES BHKW</b> — das zweite Bild des BHKW-Reiters unter der Wärmelast
    /// (<c>Bilder.BhkwStrom</c>): die Stromproduktion als Säule, Einspeisung, Reststrombedarf und
    /// Strombedarf als Linien darüber, in den Farbrollen des Stromgangs (BHKW-Strom, Überschuss,
    /// Rest, Bedarf). Gezeichnet wird über <c>ChartRenderer.ErzeugerStapel</c> wie die Wärmelast;
    /// die Reihen sind synthetisch und stehen zueinander wie im Lauf: die Einspeisung ist der Teil
    /// der Produktion über dem Bedarf, der Reststrom der Teil des Bedarfs über der Produktion.
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht
    /// sehen: dass die Abwahl einer Reihe im Bild ankommt und dass „sortiert“ die Dauerlinie
    /// zeichnet. Die SVG-Probe prüft das Modell der Zeichenfläche.</para>
    /// </summary>
    internal static partial class Program
    {
        private const string BHKW_STROM_TITEL = "Stromlast Jahresganglinie";
        private const string BHKW_STROM_Y = "Leistung [kW]";

        /// <summary>Die vier Reihen [kWh je Stunde]: Produktion, Einspeisung, Reststrom, Bedarf.</summary>
        private static (double[] Produktion, double[] Einspeisung, double[] Reststrom, double[] Bedarf) BhkwStromreihen()
        {
            double[] produktion = Jahresreihe(40, 25, 5, 0, Math.PI / 2);
            double[] bedarf = Jahresreihe(45, 10, 20, 3, Math.PI / 3);
            var einspeisung = new double[STUNDEN];
            var rest = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
            {
                einspeisung[h] = Math.Max(0, produktion[h] - bedarf[h]);
                rest[h] = Math.Max(0, bedarf[h] - produktion[h]);
            }
            return (produktion, einspeisung, rest, bedarf);
        }

        private static Zeichenmodell BhkwStromModell(bool sortiert, bool mitEinspeisung = true)
        {
            var r = BhkwStromreihen();
            var stapel = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe("Stromproduktion", r.Produktion, Farbrolle.STROM_BHKW,
                                        ChartRenderer.Stapelart.Saeule, ChartRenderer.Strichart.Durchgezogen,
                                        sortiert ? 4f : 0f)
            };
            var linien = new List<ChartRenderer.Reihe>();
            if (mitEinspeisung)
                linien.Add(new ChartRenderer.Reihe("Stromeinspeisung", r.Einspeisung, Farbrolle.UEBERSCHUSS));
            linien.Add(new ChartRenderer.Reihe("Reststrombedarf", r.Reststrom, Farbrolle.REST));
            linien.Add(new ChartRenderer.Reihe("Strombedarf", r.Bedarf, Farbrolle.BEDARF));
            return ChartRenderer.ErzeugerStapelModell(BHKW_STROM_TITEL, stapel, linien, null, BHKW_STROM_Y,
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate, sortiert);
        }

        private static byte[] BhkwStrombild(bool sortiert, bool mitEinspeisung = true)
            => SkiaMaler.Png(BhkwStromModell(sortiert, mitEinspeisung));

        private static void BhkwStromProben(string ziel)
        {
            var farben = new[]
            {
                Rollenfarbe(Farbrolle.STROM_BHKW), Rollenfarbe(Farbrolle.UEBERSCHUSS),
                Rollenfarbe(Farbrolle.REST), Rollenfarbe(Farbrolle.BEDARF)
            };

            // Maßproben: die Ganglinie über den Monaten und die Dauerlinie über den Jahresstunden.
            Pruefe(ziel, "bhkw_strom_ganglinie", 1240, 560, farben, () => BhkwStrombild(false));
            Pruefe(ziel, "bhkw_strom_sortiert", 1240, 560, farben, () => BhkwStrombild(true));

            // Gegenproben: die Abwahl der Einspeisung und „sortiert“ kommen im Bild an.
            Unterschiedlich("bhkw_strom_reihenwahl_wirkt", () => BhkwStrombild(false), () => BhkwStrombild(false, false));
            Unterschiedlich("bhkw_strom_sortiert_wirkt", () => BhkwStrombild(false), () => BhkwStrombild(true));

            // SVG-Probe: das Modell der Zeichenfläche (Flächen, Linien, Datenreihen).
            SvgModellprobe("bhkw_strom_ganglinie", () => BhkwStromModell(false));
        }
    }
}
