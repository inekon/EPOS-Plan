using System;
using System.Collections.Generic;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DIE VORSCHAUBILDER DES ZAPFPROFILS</b> (Umsetzungskonzept Zapfprofilgenerator 5.6;
    /// Stufe Z1, Gruppe 3): der Tagesgang je Tagtyp, das Wochenprofil über 168 Stunden und der
    /// Jahresgang mit Zirkulation — die Zeichenbausteine <c>ZapfprofilBilder</c> über das neue
    /// <c>ChartRenderer.StundenprofileModell</c> und den vorhandenen Monatsstapel.
    ///
    /// <para><b>Synthetische Reihen</b> (keine Datenbank, kein Zufall): Der Werktag trägt zwei
    /// Spitzen um 7 und 19 Uhr, Samstag und Sonntag dieselbe Form später und breiter, die
    /// Zirkulation eine Konstante im Laufzeitfenster; der Jahresgang eine Kosinuswelle mit dem
    /// Größtwert im Januar. Die Texte sind die deutsche Vorgabe
    /// (<c>ZapfprofilBildtexte</c>) — kein Probebild hängt an der Oberflächensprache.</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht
    /// sehen: dass eine zweite Reihe gezeichnet wird, dass die Strichart der Zirkulation wirkt
    /// und dass die Zirkulation im Jahresgang als Schicht ankommt.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Die Farbe der Zapfung in der Vorgabepalette (Rolle WARMWASSER, DeepSkyBlue).</summary>
        private static readonly SKColor ZPG_ZAPFUNG = new SKColor(0x00, 0xBF, 0xFF);

        /// <summary>Samstag (SERIE_6), Sonn-/Feiertag (SERIE_5), Zirkulation (SERIE_7).</summary>
        private static readonly SKColor ZPG_SAMSTAG = new SKColor(0x2E, 0x8B, 0x8B);
        private static readonly SKColor ZPG_SONNTAG = new SKColor(0x7A, 0x5C, 0xA8);
        private static readonly SKColor ZPG_ZIRKULATION = new SKColor(0xC0, 0x50, 0x4D);

        /// <summary>Die Proben der Zapfprofil-Vorschau — eine Zeile in <c>Program.cs</c> ruft sie.</summary>
        private static void ZapfprofilProben(string ziel)
        {
            var texte = new ZapfprofilBildtexte();
            double[] werktag = Tagesgang(7.0, 19.0, 1.2, 12.0);
            double[] samstag = Tagesgang(9.0, 20.0, 2.0, 9.0);
            double[] sonntag = Tagesgang(10.0, 19.0, 2.6, 8.0);
            double[] zirk = Zirkulationstag(1.4, 5, 22);
            double[] woche = Woche(werktag, samstag, sonntag);
            double[] wocheZirk = Woche(zirk, zirk, zirk);
            double[] monate = Monatsgang(3.6, 0.6);
            double[] monateZirk = Konstant(12, 1.0);

            // ---- Maßproben -------------------------------------------------------

            // Drei Tagtypen und die Zirkulation: eine Legendenzeile, 1244 x 524.
            Pruefe(ziel, "zapfprofil_tagesgang", 1244, 524,
                   new[] { ZPG_ZAPFUNG, ZPG_SAMSTAG, ZPG_SONNTAG },
                   () => ZapfprofilBilder.Tagesgang("Januar", werktag, samstag, sonntag, zirk, texte));

            // 168 Wochenstunden, Teilung alle 24 h, Zirkulation gestrichelt.
            Pruefe(ziel, "zapfprofil_wochenprofil", 1244, 524,
                   new[] { ZPG_ZAPFUNG },
                   () => ZapfprofilBilder.Wochenprofil(woche, wocheZirk, texte));

            // Zwölf Monatssäulen, Zapfung unten, Zirkulation darüber (Monatsstapel 978 x 542).
            Pruefe(ziel, "zapfprofil_jahresgang", 978, 542,
                   new[] { ZPG_ZAPFUNG, ZPG_ZIRKULATION },
                   () => ZapfprofilBilder.Jahresgang(monate, monateZirk, "MWh", texte));

            // Keine gültige Reihe: der Leerhinweis, 1244 x 464 wie das Stundenprofil.
            Pruefe(ziel, "stundenprofile_leer", 1244, 464,
                   new SKColor[0],
                   () => ChartRenderer.Stundenprofile("Leer", new List<ChartRenderer.Reihe>
                   {
                       null,
                       new ChartRenderer.Reihe("zu kurz", new[] { 1.0 }, ZapfprofilBilder.RolleZapfung),
                       new ChartRenderer.Reihe("nicht endlich", new[] { 1.0, double.NaN }, ZapfprofilBilder.RolleZapfung)
                   }, 6, "Stunde", "Leistung [kW]"));

            // Drei lange Reihennamen: Zwei Einträge passen in eine Zeile, der dritte bricht um;
            // die Zeichenfläche rückt nach unten, das Bild wird um die Zeile (30 px) höher. Die
            // Namen sind so lang gewählt, dass die Zeilenzahl auch bei den Textbreiten einer
            // anderen Plattform dieselbe bleibt (zwei Einträge deutlich unter, drei deutlich
            // über der Breite der Legende).
            Pruefe(ziel, "stundenprofile_legende_umbruch", 1244, 554,
                   new[] { ZPG_ZAPFUNG },
                   () => ChartRenderer.Stundenprofile("Drei Reihen", DreiReihen(werktag), 6,
                                                      "Stunde", "Leistung [kW]"));

            // ---- Gegenproben -----------------------------------------------------

            // Erstens die ZWEITE Reihe: derselbe Werktag, einmal mit, einmal ohne Samstag.
            Unterschiedlich("stundenprofile_zweite_reihe_wirkt",
                () => ZapfprofilBilder.Tagesgang("Januar", werktag, samstag, null, null, texte),
                () => ZapfprofilBilder.Tagesgang("Januar", werktag, null, null, null, texte));

            // Zweitens die STRICHART: dieselbe Zirkulation gestrichelt und durchgezogen.
            Unterschiedlich("stundenprofile_strichart_wirkt",
                () => ChartRenderer.Stundenprofile("Strichart", new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe("Werktag", werktag, ZapfprofilBilder.RolleZapfung),
                    new ChartRenderer.Reihe("Zirkulation", zirk, ZapfprofilBilder.RolleZirkulation,
                        ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Gestrichelt)
                }, 6, "Stunde", "Leistung [kW]"),
                () => ChartRenderer.Stundenprofile("Strichart", new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe("Werktag", werktag, ZapfprofilBilder.RolleZapfung),
                    new ChartRenderer.Reihe("Zirkulation", zirk, ZapfprofilBilder.RolleZirkulation)
                }, 6, "Stunde", "Leistung [kW]"));

            // Drittens die ZIRKULATION im Jahresgang: mit Schicht und ohne.
            Unterschiedlich("zapfprofil_jahresgang_zirkulation_wirkt",
                () => ZapfprofilBilder.Jahresgang(monate, monateZirk, "MWh", texte),
                () => ZapfprofilBilder.Jahresgang(monate, null, "MWh", texte));

            // ---- SVG-Proben ------------------------------------------------------

            // Tagesgang und Wochenprofil tragen eine Zeichenfläche und je Reihe eine
            // Datenreihe (die erste als Fläche) - die Modellprobe der Gruppe (a).
            SvgModellprobe("zapfprofil_tagesgang",
                () => ZapfprofilBilder.TagesgangModell("Januar", werktag, samstag, sonntag, zirk, texte));
            SvgModellprobe("zapfprofil_wochenprofil",
                () => ZapfprofilBilder.WochenprofilModell(woche, wocheZirk, texte));

            // Der Jahresgang ist ein Monatsstapel und damit ein reines Pixelbild (Gruppe (c)).
            SvgPixelbildprobe("zapfprofil_jahresgang",
                () => ZapfprofilBilder.JahresgangModell(monate, monateZirk, "MWh", texte));

            // ---- Stufe Z4: die Dauerlinie ------------------------------------------
            (double[] linie, int[] perz, int[] raenge, double[] werte) = Dauerlinienprobe(woche, wocheZirk);

            // 8760 geordnete Stunden als Fläche, vier Perzentilmarken, die Vergleichslinie gestrichelt.
            Pruefe(ziel, "zapfprofil_dauerlinie", 1244, 524,
                   new[] { ZPG_ZAPFUNG, ZPG_VERSORGUNG },
                   () => ZapfprofilBilder.Dauerlinie(linie, perz, raenge, werte, 9.0, "Ladeleistung", texte));

            // Gegenprobe: dieselbe Linie mit und ohne Perzentilmarken.
            Unterschiedlich("zapfprofil_dauerlinie_marken_wirken",
                () => ZapfprofilBilder.Dauerlinie(linie, perz, raenge, werte, null, null, texte),
                () => ZapfprofilBilder.Dauerlinie(linie, null, null, null, null, null, texte));

            // Gegenprobe: die Vergleichslinie wird gezeichnet.
            Unterschiedlich("zapfprofil_dauerlinie_vergleich_wirkt",
                () => ZapfprofilBilder.Dauerlinie(linie, perz, raenge, werte, 9.0, "Ladeleistung", texte),
                () => ZapfprofilBilder.Dauerlinie(linie, perz, raenge, werte, null, null, texte));

            SvgModellprobe("zapfprofil_dauerlinie",
                () => ZapfprofilBilder.DauerlinieModell(linie, perz, raenge, werte, 9.0, "Ladeleistung", texte));
        }

        /// <summary>
        /// Eine Dauerlinie aus 8760 Stunden: die Probewoche samt Zirkulation, über das Jahr
        /// wiederholt und absteigend geordnet; die Marken P50, P90, P95, P99 nach dem Rangverfahren
        /// (aufsteigend Rang ⌈p/100 · 8760⌉), ihr Rang auf der absteigenden Linie.
        /// </summary>
        private static (double[] Linie, int[] Perzentile, int[] Raenge, double[] Werte) Dauerlinienprobe(double[] woche,
                                                                                                         double[] zirk)
        {
            const int N = 8760;
            var werte = new double[N];
            for (int h = 0; h < N; h++) werte[h] = woche[h % 168] + zirk[h % 168];
            Array.Sort(werte);
            int[] perz = { 50, 90, 95, 99 };
            var raenge = new int[perz.Length];
            var marke = new double[perz.Length];
            for (int i = 0; i < perz.Length; i++)
            {
                int rang = (int)Math.Ceiling(perz[i] / 100.0 * N);
                marke[i] = werte[rang - 1];
                raenge[i] = N - rang + 1;
            }
            Array.Reverse(werte);
            return (werte, perz, raenge, marke);
        }

        /// <summary>Ein Tagesgang: Grundlast plus zwei Glocken um <paramref name="morgen"/> und <paramref name="abend"/> Uhr [kW].</summary>
        private static double[] Tagesgang(double morgen, double abend, double breite, double spitze)
        {
            var w = new double[24];
            for (int h = 0; h < 24; h++)
            {
                double x = h + 0.5;
                w[h] = 0.4
                       + spitze * Math.Exp(-Math.Pow((x - morgen) / breite, 2))
                       + 0.7 * spitze * Math.Exp(-Math.Pow((x - abend) / breite, 2));
            }
            return w;
        }

        /// <summary>Die Zirkulation eines Tages: <paramref name="kw"/> von <paramref name="von"/> bis vor <paramref name="bis"/> Uhr.</summary>
        private static double[] Zirkulationstag(double kw, int von, int bis)
        {
            var w = new double[24];
            for (int h = von; h < bis; h++) w[h] = kw;
            return w;
        }

        /// <summary>Eine Woche Montag bis Sonntag aus fünf Werktagen, einem Samstag und einem Sonntag.</summary>
        private static double[] Woche(double[] werktag, double[] samstag, double[] sonntag)
        {
            var w = new double[168];
            for (int t = 0; t < 7; t++)
            {
                double[] tag = t < 5 ? werktag : t == 5 ? samstag : sonntag;
                Array.Copy(tag, 0, w, t * 24, 24);
            }
            return w;
        }

        /// <summary>Zwölf Monatswerte: Mittel plus Kosinuswelle, der Größtwert im Januar [MWh].</summary>
        private static double[] Monatsgang(double mittel, double amplitude)
        {
            var w = new double[12];
            for (int m = 0; m < 12; m++) w[m] = mittel + amplitude * Math.Cos(2.0 * Math.PI * m / 12.0);
            return w;
        }

        private static double[] Konstant(int n, double wert)
        {
            var w = new double[n];
            for (int i = 0; i < n; i++) w[i] = wert;
            return w;
        }

        /// <summary>Drei Reihen mit langen Namen — genug für genau einen Umbruch der Legende.</summary>
        private static List<ChartRenderer.Reihe> DreiReihen(double[] basis)
        {
            Farbrolle[] rollen = { ZapfprofilBilder.RolleZapfung, Farbrolle.SERIE_5, Farbrolle.SERIE_6 };
            var reihen = new List<ChartRenderer.Reihe>();
            for (int k = 0; k < rollen.Length; k++)
            {
                var w = new double[basis.Length];
                for (int h = 0; h < w.Length; h++) w[h] = basis[h] * (1.0 - 0.1 * k);
                reihen.Add(new ChartRenderer.Reihe("Nutzungszone mit sehr langem Namen " + (k + 1), w, rollen[k]));
            }
            return reihen;
        }
    }
}
