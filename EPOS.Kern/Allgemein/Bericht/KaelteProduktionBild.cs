using System;
using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Jahresganglinie „Kälteproduktion" des Unterreiters „Kälte Produktion Chart" — das
    /// Kälte-Gegenstück zur Wärmeproduktion des Ergebnisreiters.
    ///
    /// <para><b>Reihen:</b> je Kälteerzeuger (Wärmepumpe im Kühlbetrieb) seine gedeckte Kälte je
    /// Stunde als Säule, darauf die ungedeckte Kälte im Grau des Rests; darüber der Kältebedarf als
    /// Linie auf derselben Achse. Die Stapelhöhe ist damit in jeder Stunde der Kältebedarf.</para>
    ///
    /// <para><b>Keine neue Rechnung:</b> Die Reihen sind die Stundenreihen des Laufs —
    /// <see cref="SimulationKaeltebedarf.Kaeltebedarf"/>, <see cref="Kaelteerzeuger.Kaelte_stuendlich"/>
    /// und <see cref="Kaeltekaskade.Rest_stuendlich"/>. Ohne Kälteerzeuger ist der ganze Bedarf
    /// ungedeckt; rechnet das Projekt keine Kälte, gibt es kein Bild.</para>
    /// </summary>
    public static class KaelteProduktionBild
    {
        /// <summary>Die Beschriftungen; die Vorgaben sind der deutsche Rückfall.</summary>
        public sealed class Texte
        {
            public string Titel { get; set; } = "Kälteproduktion Jahresganglinie";
            public string Achse { get; set; } = "Leistung [kW]";
            public string Bedarf { get; set; } = "Kältebedarf";
            public string Rest { get; set; } = "Ungedeckte Kälte";
            public string Erzeuger { get; set; } = "Wärmepumpe";
        }

        /// <summary>Eine Erzeugerreihe: Bezeichner und gedeckte Kälte je Stunde [kWh].</summary>
        public sealed class Erzeugerreihe
        {
            public string Name { get; set; } = "";
            public double[] Werte { get; set; } = new double[0];
        }

        /// <summary>Die Stundenreihen des Bildes, aus dem Lauf übernommen.</summary>
        public sealed class Reihen
        {
            /// <summary>Kältebedarf je Stunde [kWh].</summary>
            public double[] Bedarf = new double[0];

            /// <summary>Je Kälteerzeuger die gedeckte Kälte je Stunde, in der Folge der Kaskade.</summary>
            public List<Erzeugerreihe> Erzeuger = new List<Erzeugerreihe>();

            /// <summary>Ungedeckte Kälte je Stunde [kWh].</summary>
            public double[] Rest = new double[0];
        }

        /// <summary>Die Farbrollen der Erzeuger in der Folge der Kaskade.</summary>
        private static readonly Farbrolle[] ROLLEN_ERZEUGER =
        {
            Farbrolle.WAERME_WP, Farbrolle.SERIE_2, Farbrolle.SERIE_3, Farbrolle.SERIE_4, Farbrolle.SERIE_5
        };

        /// <summary>
        /// Die Reihen aus der Kälteseite eines Laufs; <c>null</c>, wenn das Projekt keine Kälte
        /// rechnet (<see cref="SimulationKaeltebedarf.Gerechnet"/> = false). Ohne Kältekaskade
        /// (kein Kälteerzeuger) ist der Rest der ganze Bedarf.
        /// </summary>
        public static Reihen AusLauf(SimulationKaeltebedarf k)
        {
            if (k == null || !k.Gerechnet) return null;

            var r = new Reihen { Bedarf = Kopie(k.Kaeltebedarf) };
            Kaeltekaskade kaskade = k.Kaskade;
            if (kaskade == null || kaskade.Erzeuger.Count == 0)
            {
                r.Rest = Kopie(k.Kaeltebedarf);
                return r;
            }

            foreach (Kaelteerzeuger z in kaskade.Erzeuger)
                r.Erzeuger.Add(new Erzeugerreihe { Name = z.Bezeichner ?? "", Werte = Kopie(z.Kaelte_stuendlich) });
            r.Rest = Kopie(kaskade.Rest_stuendlich);
            return r;
        }

        /// <summary>
        /// Die Stapel- und Linienreihen des Bildes: je Erzeuger eine Säule (leerer Bezeichner →
        /// <see cref="Texte.Erzeuger"/>), danach die ungedeckte Kälte — nur, wenn sie im Jahr etwas
        /// trägt oder kein Erzeuger rechnet —, und als Linie der Kältebedarf.
        /// </summary>
        public static (List<ChartRenderer.Reihe> Stapel, List<ChartRenderer.Reihe> Linien) Reihenbilden(
            Reihen r, Texte texte, bool sortiert)
        {
            texte ??= new Texte();
            var stapel = new List<ChartRenderer.Reihe>();
            var linien = new List<ChartRenderer.Reihe>();
            if (r == null) return (stapel, linien);

            float breite = sortiert ? 4f : 0f;
            for (int i = 0; i < r.Erzeuger.Count; i++)
            {
                Erzeugerreihe e = r.Erzeuger[i];
                string name = string.IsNullOrEmpty(e.Name) ? texte.Erzeuger : e.Name;
                stapel.Add(new ChartRenderer.Reihe(name, Kopie(e.Werte), ROLLEN_ERZEUGER[i % ROLLEN_ERZEUGER.Length],
                                                   ChartRenderer.Stapelart.Saeule, ChartRenderer.Strichart.Durchgezogen,
                                                   breite));
            }

            if (r.Erzeuger.Count == 0 || Summe(r.Rest) > 0)
                stapel.Add(new ChartRenderer.Reihe(texte.Rest, Kopie(r.Rest), Farbrolle.REST,
                                                   ChartRenderer.Stapelart.Saeule, ChartRenderer.Strichart.Durchgezogen,
                                                   breite));

            linien.Add(new ChartRenderer.Reihe(texte.Bedarf, Kopie(r.Bedarf), Farbrolle.BEDARF,
                                               ChartRenderer.Stapelart.Keine, ChartRenderer.Strichart.Durchgezogen, 2f));
            return (stapel, linien);
        }

        /// <summary>Das Bild als Zeichenmodell; <c>null</c> ohne Reihen.</summary>
        public static Zeichenmodell Modell(Reihen r, Texte texte = null, bool sortiert = false)
        {
            if (r == null) return null;
            texte ??= new Texte();
            var (stapel, linien) = Reihenbilden(r, texte, sortiert);
            return ChartRenderer.ErzeugerStapelModell(
                texte.Titel, stapel, linien, null, texte.Achse,
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate, sortiert);
        }

        /// <summary>Dasselbe Bild als PNG (Prüfstand).</summary>
        public static byte[] Png(Reihen r, Texte texte = null, bool sortiert = false)
        {
            Zeichenmodell z = Modell(r, texte, sortiert);
            return z == null ? null : SkiaMaler.Png(z);
        }

        private static double[] Kopie(double[] werte) => werte == null ? new double[0] : (double[])werte.Clone();

        private static double Summe(double[] werte)
        {
            double s = 0;
            if (werte != null) foreach (double w in werte) s += w;
            return s;
        }
    }
}
