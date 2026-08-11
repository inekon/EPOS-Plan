using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Reiner Rechenkern der Kapitalwertmethode (DIN EN 17463 / ValERI; Konzept
    /// Kap. 5.1) — ohne DB- und UI-Abhängigkeit, dadurch gegen Referenzwerte
    /// testbar (goetz_test.XLS, VALERI_Vorlage_V7).
    ///
    ///   KW = −I₀ + Σ_{t=1..T} (E_t − A_t) / (1+i)^t + RW_T / (1+i)^T
    ///
    /// Regeln der Ausbaustufe W1:
    ///  - Ersatzbeschaffung: Position mit Nutzungsdauer n &lt; T wird in t = n, 2n, …
    ///    (t &lt; T) nominal unverändert erneut beschafft (keine Preissteigerung auf
    ///    Investitionen — Vereinfachung W1, im Bericht ausgewiesen).
    ///  - Restwert linear: je Position Investition × Restnutzungsdauer/Nutzungsdauer
    ///    zum Zeitpunkt T, abgezinst (Entscheidung 11.08.2026).
    ///  - Nutzungsdauer &lt; 1 a wird wie n = T behandelt (keine Ersatzbeschaffung,
    ///    kein Restwert) — betrifft Positionen ohne (sinnvoll) gepflegte Nutzungsdauer.
    ///  - Betriebskosten steigen mit p_B, Energiekosten mit p_E [%/a];
    ///    Einspeiseerlöse bleiben nominal konstant (feste Vergütung, W1).
    /// </summary>
    public static class KapitalwertRechner
    {
        /// <summary>Eine Investitionsposition (Tab_ProjektWerte, Kategorie 1, Szenariowert).</summary>
        public class InvestPosition
        {
            public double Betrag;          // [€]
            public double Nutzungsdauer;   // [a]; < 1 → wie Betrachtungszeitraum
        }

        /// <summary>Zahlungsstrombild eines Projekts über den Betrachtungszeitraum.</summary>
        public class Zahlungsbild
        {
            public double Investition;          // I₀ [€]
            public double BarwertAusgaben;      // Betrieb + Energie + Ersatz [€]
            public double BarwertEinnahmen;     // Erlöse [€]
            public double RestwertBarwert;      // [€]
            public double Kapitalwert;          // KW [€]

            /// <summary>Barwert-Zahlungsreihe: Index 0 = −I₀, Index t = Barwert des
            /// Netto-Zahlungsstroms im Jahr t OHNE Restwert (für Amortisation).</summary>
            public double[] BarwertReihe;
        }

        /// <summary>Annuitätenfaktor a(i,n); i als Dezimalzahl (0,03), n in Jahren.</summary>
        public static double Annuitaet(double i, double n)
        {
            if (n <= 0) return 0;
            if (Math.Abs(i) < 1e-12) return 1.0 / n;
            double q = Math.Pow(1.0 + i, n);
            return i * q / (q - 1.0);
        }

        /// <summary>
        /// Absolutes Zahlungsbild eines Projekts. Kostenreihen in €/a (Jahr-1-Werte),
        /// Zins/Preissteigerungen in Prozent. energieJahr = null → Energiekosten
        /// unbestimmbar; der Aufrufer setzt dann Fehlgrund und lässt KW leer.
        /// </summary>
        public static Zahlungsbild Rechne(List<InvestPosition> investitionen,
                                          double betriebJahr, double energieJahr, double erloesJahr,
                                          double zinsProzent, int jahre,
                                          double preisstBetriebProzent, double preisstEnergieProzent)
        {
            double i = zinsProzent / 100.0;
            double pB = preisstBetriebProzent / 100.0;
            double pE = preisstEnergieProzent / 100.0;
            int T = Math.Max(1, jahre);

            var z = new Zahlungsbild { BarwertReihe = new double[T + 1] };

            // ---------------- Investition t=0 + Ersatzbeschaffungen + Restwert ----------------
            double[] ersatzJeJahr = new double[T + 1];
            double restwertT = 0;

            if (investitionen != null)
            {
                foreach (InvestPosition pos in investitionen)
                {
                    if (pos == null || pos.Betrag == 0) continue;
                    // Nutzungsdauern < 1 a sind fachlich nicht sinnvoll → wie T behandeln
                    // (verhindert zugleich exzessive Ersatz-Schleifen bei Fehleingaben).
                    double n = pos.Nutzungsdauer >= 1.0 ? pos.Nutzungsdauer : T;
                    z.Investition += pos.Betrag;

                    // Ersatz auf ganze Jahre gerundet: tj = round(k·n), 1 ≤ tj < T
                    // (im letzten Betrachtungsjahr wird nicht mehr ersetzt).
                    int letzteBeschaffung = 0;
                    for (double t = n; ; t += n)
                    {
                        int tj = (int)Math.Round(t);
                        if (tj >= T) break;
                        if (tj >= 1) { ersatzJeJahr[tj] += pos.Betrag; letzteBeschaffung = tj; }
                    }

                    // Linearer Restwert der letzten Beschaffung zum Zeitpunkt T
                    // (konsistent zum gerundeten Buchungsjahr).
                    double alter = T - letzteBeschaffung;
                    double rest = n - alter;
                    if (rest > 1e-9) restwertT += pos.Betrag * (rest / n);
                }
            }

            // ---------------- Jahresreihe abzinsen ----------------
            z.BarwertReihe[0] = -z.Investition;
            for (int t = 1; t <= T; t++)
            {
                double faktor = Math.Pow(1.0 + i, -t);
                double ausgaben = betriebJahr * Math.Pow(1.0 + pB, t - 1)
                                + energieJahr * Math.Pow(1.0 + pE, t - 1)
                                + ersatzJeJahr[t];
                double einnahmen = erloesJahr;   // W1: feste Vergütung, nominal konstant

                z.BarwertAusgaben += ausgaben * faktor;
                z.BarwertEinnahmen += einnahmen * faktor;
                z.BarwertReihe[t] = (einnahmen - ausgaben) * faktor;
            }

            z.RestwertBarwert = restwertT * Math.Pow(1.0 + i, -T);
            z.Kapitalwert = -z.Investition - z.BarwertAusgaben + z.BarwertEinnahmen + z.RestwertBarwert;
            return z;
        }

        /// <summary>
        /// Dynamische Amortisation der DIFFERENZ Variante − Stamm (Referenz = Stamm,
        /// Entscheidung 11.08.2026): erstes Jahr, in dem der kumulierte Barwert der
        /// Differenz-Zahlungsreihe ≥ 0 wird — ohne Restwert, mit linearer
        /// Interpolation im Jahr. null = amortisiert sich im Betrachtungszeitraum nie.
        /// </summary>
        public static double? AmortisationDifferenz(Zahlungsbild variante, Zahlungsbild stamm)
        {
            if (variante == null || stamm == null) return null;
            int T = Math.Min(variante.BarwertReihe.Length, stamm.BarwertReihe.Length) - 1;

            double kum = variante.BarwertReihe[0] - stamm.BarwertReihe[0];   // −ΔI₀
            if (kum >= 0)
            {
                // Keine Mehrinvestition: „amortisiert ab Jahr 0" gilt nur, wenn die
                // Variante auch über den Zeitraum netto nicht schlechter fährt.
                double summe = kum;
                for (int t = 1; t <= T; t++) summe += variante.BarwertReihe[t] - stamm.BarwertReihe[t];
                return summe >= 0 ? (double?)0 : null;
            }
            for (int t = 1; t <= T; t++)
            {
                double zufluss = variante.BarwertReihe[t] - stamm.BarwertReihe[t];
                if (kum + zufluss >= 0 && zufluss > 0)
                    return (t - 1) + (-kum / zufluss);                       // Interpolation im Jahr t
                kum += zufluss;
            }
            return null;
        }
    }
}
