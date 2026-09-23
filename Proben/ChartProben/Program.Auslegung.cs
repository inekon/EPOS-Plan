using System;
using System.Collections.Generic;
using SkiaSharp;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;

namespace ChartProben
{
    /// <summary>
    /// <b>DIE BILDER DER AUSLEGUNG</b> (Umsetzungskonzept Zapfprofilgenerator 5.6; Stufe Z2,
    /// Gruppe 2): die Summenlinie des Bedarfstags mit Speicherinhalt und kleinstem Abstand, die
    /// Wertepaarkurve Volumen über Leistung mit dem gewählten Punkt und die maßgebende Woche der
    /// Stundenbilanz mit Defizit und Füllstand auf der zweiten Achse — die Zeichenbausteine
    /// <c>ZapfprofilBilder</c> über das neue <c>ChartRenderer.SummenlinieModell</c>.
    ///
    /// <para><b>Synthetische Reihen</b> (keine Datenbank, kein Zufall): ein Bedarfstag aus zwei
    /// Zapfblöcken, eine Versorgung aus Speicherinhalt und zwei Ladephasen, fünf erfundene
    /// Wertepaare, die Woche aus den Tagesgängen der Vorschauproben. Die Texte sind die deutsche
    /// Vorgabe (<c>ZapfprofilAuslegungBildtexte</c>).</para>
    ///
    /// <para><b>Die Gegenproben</b> halten fest, was Maß-, Farb- und Determinismusprüfung nicht
    /// sehen: dass die Marken gezeichnet werden, dass die zweite Achse ankommt und dass eigene
    /// x-Stellen die Kurve verschieben.</para>
    /// </summary>
    internal static partial class Program
    {
        /// <summary>Die Versorgung und die Ladung (Rolle SPEICHERLADUNG, DarkOrange).</summary>
        private static readonly SKColor ZPG_VERSORGUNG = new SKColor(0xFF, 0x8C, 0x00);

        /// <summary>Die Wertepaarkurve (Rolle STAMM).</summary>
        private static readonly SKColor ZPG_WERTEPAARE = new SKColor(0x1F, 0x4E, 0x79);

        /// <summary>Das Defizit (SERIE_4) und der Füllstand (SPEICHERFUELLSTAND).</summary>
        private static readonly SKColor ZPG_DEFIZIT = new SKColor(0x9E, 0x48, 0x0E);
        private static readonly SKColor ZPG_FUELLSTAND = new SKColor(0x78, 0x82, 0x8C);

        /// <summary>Die Proben der Auslegungsbilder — eine Zeile in <c>Program.cs</c> ruft sie.</summary>
        private static void AuslegungProben(string ziel)
        {
            var texte = new ZapfprofilAuslegungBildtexte();
            (double[] bedarf, double[] versorgung) = Summenlinienprobe();
            double[] leistung = { 5.0, 10.0, 15.0, 20.0, 25.0 };
            double[] volumen = { 600.0, 420.0, 330.0, 280.0, 260.0 };
            double[] zapfung = Woche(Tagesgang(7.0, 19.0, 1.2, 12.0), Tagesgang(9.0, 20.0, 2.0, 9.0), Tagesgang(10.0, 19.0, 2.6, 8.0));
            double[] zirk = Woche(Zirkulationstag(1.4, 5, 22), Zirkulationstag(1.4, 5, 22), Zirkulationstag(1.4, 5, 22));
            double[] ladung = Woche(Zirkulationstag(9.0, 0, 8), Zirkulationstag(9.0, 0, 8), Zirkulationstag(9.0, 0, 8));
            (double[] defizit, double[] fuell) = Defizitprobe(zapfung, zirk, ladung, 40.0);

            // ---- Maßproben -------------------------------------------------------

            // Bedarf und Versorgung über 1 441 Minutenwerte, Speicherinhalt und kleinster Abstand.
            Pruefe(ziel, "zapfprofil_summenlinie", 1244, 524,
                   new[] { ZPG_ZAPFUNG, ZPG_VERSORGUNG },
                   () => ZapfprofilBilder.Summenlinie(bedarf, versorgung, 700, texte));

            // Fünf Wertepaare, x die Leistung, der gewählte Punkt als Kreis.
            Pruefe(ziel, "zapfprofil_wertepaarkurve", 1244, 524,
                   new[] { ZPG_WERTEPAARE },
                   () => ZapfprofilBilder.Wertepaarkurve(leistung, volumen, 15.0, 330.0, texte));

            // Die Woche: Zapfung als Fläche, Zirkulation und Ladung, Defizit und Füllstand rechts.
            Pruefe(ziel, "zapfprofil_auslegungswoche", 1244, 524,
                   new[] { ZPG_ZAPFUNG, ZPG_VERSORGUNG, ZPG_DEFIZIT, ZPG_FUELLSTAND },
                   () => ZapfprofilBilder.Auslegungswoche(zapfung, zirk, ladung, defizit, fuell, 500.0, 32, texte));

            // Keine gültige Reihe: der Leerhinweis, 1244 x 464.
            Pruefe(ziel, "summenlinie_leer", 1244, 464,
                   new SKColor[0],
                   () => ChartRenderer.Summenlinie("Leer", new List<ChartRenderer.Reihe>
                   {
                       null,
                       new ChartRenderer.Reihe("zu kurz", new[] { 1.0 }, ZapfprofilBilder.RolleZapfung),
                       new ChartRenderer.Reihe("nicht endlich", new[] { 1.0, double.PositiveInfinity }, ZapfprofilBilder.RolleZapfung)
                   }, null, 0.0, 1.0, "x", "y"));

            // ---- Gegenproben -----------------------------------------------------

            // Erstens die MARKEN: dieselbe Summenlinie mit und ohne Speicherinhalt und Punkt.
            Unterschiedlich("summenlinie_marken_wirken",
                () => ZapfprofilBilder.Summenlinie(bedarf, versorgung, 700, texte),
                () => ChartRenderer.Summenlinie(texte.TitelSummenlinie, new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe(texte.Bedarf, bedarf, ZapfprofilBilder.RolleZapfung),
                    new ChartRenderer.Reihe(texte.Versorgung, versorgung, ZapfprofilBilder.RolleVersorgung)
                }, null, 360.0, 60.0, texte.AchseMinutentakt, texte.AchseEnergie));

            // Zweitens die ZWEITE ACHSE: dieselbe Woche mit und ohne Defizit und Füllstand.
            Unterschiedlich("summenlinie_zweite_achse_wirkt",
                () => ZapfprofilBilder.Auslegungswoche(zapfung, zirk, ladung, defizit, fuell, 500.0, 32, texte),
                () => ZapfprofilBilder.Auslegungswoche(zapfung, zirk, ladung, null, null, null, 32, texte));

            // Drittens die EIGENEN x-STELLEN: dieselben Werte über ungleichmäßiger Leistung und über dem Index.
            Unterschiedlich("summenlinie_xwerte_wirken",
                () => ChartRenderer.Summenlinie("x", new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe("Wertepaare", volumen, ZapfprofilBilder.RolleWertepaare)
                }, new[] { 5.0, 6.0, 8.0, 15.0, 25.0 }, 0.0, 1.0, "Leistung [kW]", "Speichervolumen [l]"),
                () => ChartRenderer.Summenlinie("x", new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe("Wertepaare", volumen, ZapfprofilBilder.RolleWertepaare)
                }, null, 0.0, 1.0, "Leistung [kW]", "Speichervolumen [l]"));

            // ---- SVG-Proben ------------------------------------------------------

            // Alle drei tragen eine Zeichenfläche und je Reihe eine Datenreihe; die Woche
            // zusätzlich die Fläche der Zapfung und die zweite Achse.
            SvgModellprobe("zapfprofil_summenlinie",
                () => ZapfprofilBilder.SummenlinieModell(bedarf, versorgung, 700, texte));
            SvgModellprobe("zapfprofil_wertepaarkurve",
                () => ZapfprofilBilder.WertepaarkurveModell(leistung, volumen, 15.0, 330.0, texte));
            SvgModellprobe("zapfprofil_auslegungswoche",
                () => ZapfprofilBilder.AuslegungswocheModell(zapfung, zirk, ladung, defizit, fuell, 500.0, 32, texte));
        }

        /// <summary>
        /// Ein Bedarfstag aus zwei Zapfblöcken (7:00–7:30, 18:00–19:00) und eine Versorgung aus
        /// 30 kWh Speicherinhalt und zwei Ladephasen — kumuliert über 1 441 Minutenwerte [kWh].
        /// </summary>
        private static (double[] Bedarf, double[] Versorgung) Summenlinienprobe()
        {
            var bedarf = new double[1441];
            var versorgung = new double[1441];
            double b = 0.0, v = 30.0;
            for (int i = 0; i <= 1440; i++)
            {
                bedarf[i] = b;
                versorgung[i] = v;
                if (i == 1440) break;
                if (i >= 420 && i < 450) b += 0.4;
                if (i >= 1080 && i < 1140) b += 0.3;
                if ((i >= 450 && i < 700) || (i >= 1140 && i < 1400)) v += 0.12;
            }
            return (bedarf, versorgung);
        }

        /// <summary>
        /// Defizit und Füllstand einer Stundenbilanz über die Woche: D(t) = max(0, D(t−1) + Z + C − L),
        /// Füllstand = max(0, C_sp − D) — dieselbe Rekursion wie die Speicherauslegung, nur synthetisch.
        /// </summary>
        private static (double[] Defizit, double[] Fuellstand) Defizitprobe(double[] zapfung, double[] zirk, double[] ladung,
                                                                            double kapazitaet)
        {
            var d = new double[zapfung.Length];
            var f = new double[zapfung.Length];
            double vor = 0.0;
            for (int t = 0; t < zapfung.Length; t++)
            {
                double neu = vor + zapfung[t] + zirk[t] - ladung[t];
                vor = neu > 0 ? neu : 0.0;
                d[t] = vor;
                f[t] = Math.Max(0.0, kapazitaet - vor);
            }
            return (d, f);
        }
    }
}
