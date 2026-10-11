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

        /// <summary>
        /// Schreibt Jahresverlauf und Strombilanz aus dem stimmigen Satz, dazu Speicherverlauf, Brückenbild und
        /// Zapfprofil-Dauerlinie in Wiki-Fassung. Die Jahresdauerlinie Wärme gehört nicht dazu: Sie ordnet jede Reihe
        /// für sich, ihre Kurven können sich kreuzen und lesen sich im Wiki wie ein Fehler.
        /// </summary>
        private static void WikiZeitreihenbilder(string ordner)
        {
            ZeitreihenSatz z = WikiSatz();
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Waerme_Jahresverlauf.png"), ChartRenderer.JahresverlaufWaerme(z));
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Strombilanz_Monate.png"), ChartRenderer.StrombilanzMonate(z));
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Speicherverlauf.png"), ChartRenderer.Speicherverlauf(WikiSpeicherSatz()));
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Kapitalwert_Bruecke.png"),
                ChartRenderer.KapitalwertBruecke(WikiBrueckenschritte(), WikiBrueckentexte()));

            // Die Zapfprofil-Dauerlinie ohne Perzentilmarken (ihre Kürzel P50 bis P99 stoßen bei steiler Linie
            // aneinander), mit der Ladeleistung als Vergleichslinie.
            double[] werktag = Tagesgang(7.0, 19.0, 1.2, 12.0);
            double[] samstag = Tagesgang(9.0, 20.0, 2.0, 9.0);
            double[] sonntag = Tagesgang(10.0, 19.0, 2.6, 8.0);
            double[] zirk = Zirkulationstag(1.4, 5, 22);
            (double[] linie, _, _, _) = Dauerlinienprobe(Woche(werktag, samstag, sonntag), Woche(zirk, zirk, zirk));
            File.WriteAllBytes(Path.Combine(ordner, "Diagramm_Zapfprofil_Dauerlinie.png"),
                ZapfprofilBilder.Dauerlinie(linie, null, null, null, 9.0, "Ladeleistung", new ZapfprofilBildtexte()));
        }

        /// <summary>
        /// Drei Speicher mit stimmigem Wochengang, ohne Rollenzusatz in der Beschriftung (der Stromspeicher läuft
        /// deshalb als dritte Speicherreihe, nicht als Reihe <c>PV_SPEICHER_SOC</c>): Der Heizungspuffer pendelt im
        /// Winter weit und liegt im Sommer fast still; der Brauchwasserspeicher entlädt sich morgens und abends und
        /// wird dazwischen nachgeladen, im Sommer solar etwas höher; der Stromspeicher lädt mittags aus der
        /// Photovoltaik und entlädt sich abends, im Sommer fast voll, im Winter nur wenig.
        /// </summary>
        private static ZeitreihenSatz WikiSpeicherSatz()
        {
            var puffer = new double[STUNDEN]; var tww = new double[STUNDEN]; var batterie = new double[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
            {
                int stunde = h % 24;
                double winter = (1.0 + Math.Cos(2.0 * Math.PI * (h / (double)STUNDEN - 0.04))) / 2.0;   // 1 im Januar, 0 im Juli
                puffer[h] = 60.0 + 180.0 * winter + (15.0 + 110.0 * winter) * Math.Sin(2.0 * Math.PI * (stunde - 2) / 24.0);
                tww[h] = 130.0 + 30.0 * (1.0 - winter) + 45.0 * Math.Cos(4.0 * Math.PI * (stunde - 3) / 24.0);
                double laden = stunde >= 9 && stunde < 16 ? (stunde - 9) / 7.0
                             : stunde >= 16 ? Math.Max(0.0, 1.0 - (stunde - 16) / 7.0) : 0.0;
                batterie[h] = 10.0 + (15.0 + 70.0 * (1.0 - winter)) * laden;
            }
            var z = new ZeitreihenSatz();
            WikiSpeicher(z, "PUFFER_11", "Heizungspuffer", puffer);
            WikiSpeicher(z, "PUFFER_12", "Brauchwasserspeicher", tww);
            WikiSpeicher(z, "STROMSPEICHER_1", "Stromspeicher", batterie);
            return z;
        }

        private static void WikiSpeicher(ZeitreihenSatz z, string schluessel, string beschriftung, double[] werte)
        {
            z.Reihen[schluessel] = werte;
            z.Beschriftungen[schluessel] = beschriftung;
            z.Speicherreihen.Add(schluessel);
        }

        /// <summary>Kurze Bestandteile in runden Zahlen: mehr Investition und Betrieb, dafür weniger Energiekosten.</summary>
        private static System.Collections.Generic.List<ChartRenderer.Brueckenschritt> WikiBrueckenschritte()
            => new System.Collections.Generic.List<ChartRenderer.Brueckenschritt>
            {
                new ChartRenderer.Brueckenschritt { Name = "Investition", Wert = -400000.0 },
                new ChartRenderer.Brueckenschritt { Name = "Betriebskosten", Wert = -100000.0 },
                new ChartRenderer.Brueckenschritt { Name = "Energiekosten", Wert = 900000.0 },
                new ChartRenderer.Brueckenschritt { Name = "Erlöse", Wert = 200000.0 },
                new ChartRenderer.Brueckenschritt { Name = "Ersatzkosten", Wert = -80000.0 },
                new ChartRenderer.Brueckenschritt { Name = "Restwert", Wert = 40000.0 }
            };

        /// <summary>Die Texte des Brückenbilds ohne Formelzeichen: Ergebnis, Zinssatz und Zeitraum ausgeschrieben.</summary>
        private static ChartRenderer.BrueckenTexte WikiBrueckentexte() => new ChartRenderer.BrueckenTexte
        {
            Ergebnis = "Differenz",
            Unterzeile = "Anlage gegenüber dem Stammprojekt · Barwerte · Szenario Erwartet",
            Fuss = "Barwerte gegenüber dem Stammprojekt, Zinssatz 3 %, Betrachtungszeitraum 20 Jahre"
        };
    }
}
