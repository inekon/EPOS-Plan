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
    ///
    /// Erweiterungen der Stufe W2 (Phase 7):
    ///  - BEHG-CO₂-Abgabe: Jahr-1-Betrag [€/a], steigt mit p_E (CO₂-Preispfad).
    ///  - Zusätzliche nominale Erlösreihen je Jahr (KWKG-Bonus mit Vbh-Kontingent,
    ///    ab Etappe E4 zusätzlich die Steuergutschriften — die Jahreslogik baut der
    ///    Aufrufer, hier wird nur abgezinst).
    ///  - Nominalreihe + nominaler Restwert im Zahlungsbild → interner Zinsfuß
    ///    (IRR) der Differenzreihe per Bisektion.
    ///
    /// <para><b>ETAPPE E4 (Leitentscheidung L1): benannte Reihen statt EINER Reihe.</b>
    /// Bis dahin nahm <see cref="Rechne"/> genau ein <c>double[] zusatzErloesJeJahr</c>
    /// entgegen — die KWKG-Reihe. Mit den drei Steuergutschriften gibt es erstmals mehr
    /// als eine jahresscharfe Erlösreihe; der Parameter ist deshalb auf eine Liste
    /// benannter Reihen umgestellt (<see cref="ErloesReihe"/>). Die Rechnung selbst
    /// ändert sich nicht: Abgezinst wird die SUMME der Reihen je Jahr, und eine Liste
    /// mit genau der KWKG-Reihe liefert Wert für Wert dasselbe wie vorher. Die Namen
    /// werden im Bericht (Etappe E7) gebraucht, um die Gutschriften einzeln
    /// auszuweisen.</para>
    /// </summary>
    public static class KapitalwertRechner
    {
        /// <summary>
        /// Eine BENANNTE jahresscharfe Erlösreihe (Etappe E4, Leitentscheidung L1) —
        /// nominal, unabgezinst, Index 1…T; Index 0 bleibt unbenutzt (dort steht in den
        /// Zahlungsreihen die Investition).
        ///
        /// <para><b>Warum benannt.</b> Seit E4 gibt es vier solcher Reihen: den
        /// KWK-Zuschlag und die drei Steuergutschriften. Sie werden zwar gemeinsam
        /// abgezinst, müssen im Bericht (Etappe E7) und in der Sensitivität aber
        /// einzeln adressierbar bleiben — das Novellen-Szenario streicht zum Beispiel
        /// genau die KWKG-Reihe und lässt die Steuergutschriften stehen.</para>
        ///
        /// <para><b>Der Name ist ein Schlüssel, kein Anzeigetext</b> (Drei-Schichten-Regel):
        /// sprachneutral, ASCII, eingefroren. Die Anzeigetexte stehen in
        /// <c>MyResource.Resource.WIRT_REIHE_*</c>.</para>
        /// </summary>
        public sealed class ErloesReihe
        {
            /// <summary>KWK-Zuschlag nach KWKG 2025 (Phase 9 / Etappe E2).</summary>
            public const string KWKG = "KWKG_ZUSCHLAG";

            /// <summary>Energiesteuer-Entlastung nach § 53 bzw. § 53a EnergieStG (E4).</summary>
            public const string ENERGIESTEUER = "ENERGIESTEUER_GUTSCHRIFT";

            /// <summary>Stromsteuer-Befreiung nach § 9 Abs. 1 Nr. 3 StromStG (E4).</summary>
            public const string STROMSTEUER_BEFREIUNG = "STROMSTEUER_BEFREIUNG";

            /// <summary>Stromsteuer-Entlastung nach § 9b StromStG (E4).</summary>
            public const string STROMSTEUER_ENTLASTUNG = "STROMSTEUER_ENTLASTUNG";

            public ErloesReihe(string name, double[] jeJahr)
            {
                Name = name ?? "";
                JeJahr = jeJahr;
            }

            /// <summary>Sprachneutraler Schlüssel der Reihe.</summary>
            public string Name { get; private set; }

            /// <summary>Nominale Jahresbeträge [€/a]; Index 1…T, <c>null</c> = keine Reihe.</summary>
            public double[] JeJahr { get; private set; }

            /// <summary>Betrag des ersten Betrachtungsjahres [€/a]; 0, wenn die Reihe leer ist.</summary>
            public double Jahr1
            {
                get { return JeJahr != null && JeJahr.Length > 1 ? JeJahr[1] : 0; }
            }

            /// <summary>Wert des Jahres t [€/a]; außerhalb der Reihe 0.</summary>
            public double Wert(int t)
            {
                return JeJahr != null && t >= 0 && t < JeJahr.Length ? JeJahr[t] : 0;
            }
        }

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

            /// <summary>Nominale Zahlungsreihe (unabgezinst), gleicher Aufbau —
            /// Grundlage des internen Zinsfußes (W2).</summary>
            public double[] NominalReihe;

            /// <summary>Restwert zum Zeitpunkt T, unabgezinst (für den IRR).</summary>
            public double RestwertNominal;
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
        /// <param name="zusatzErloesReihen">
        /// Benannte jahresscharfe Erlösreihen (Etappe E4, L1) — KWK-Zuschlag und
        /// Steuergutschriften. <c>null</c> oder leer = keine. Abgezinst wird die SUMME
        /// je Jahr; eine Liste mit genau der KWKG-Reihe rechnet Wert für Wert wie der
        /// frühere Parameter <c>double[] zusatzErloesJeJahr</c>.
        /// </param>
        public static Zahlungsbild Rechne(List<InvestPosition> investitionen,
                                          double betriebJahr, double energieJahr, double erloesJahr,
                                          double zinsProzent, int jahre,
                                          double preisstBetriebProzent, double preisstEnergieProzent,
                                          double behgJahr = 0,
                                          IList<ErloesReihe> zusatzErloesReihen = null)
        {
            double i = zinsProzent / 100.0;
            double pB = preisstBetriebProzent / 100.0;
            double pE = preisstEnergieProzent / 100.0;
            int T = Math.Max(1, jahre);

            var z = new Zahlungsbild { BarwertReihe = new double[T + 1], NominalReihe = new double[T + 1] };

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
            z.NominalReihe[0] = -z.Investition;
            for (int t = 1; t <= T; t++)
            {
                double faktor = Math.Pow(1.0 + i, -t);
                double ausgaben = betriebJahr * Math.Pow(1.0 + pB, t - 1)
                                + (energieJahr + behgJahr) * Math.Pow(1.0 + pE, t - 1)
                                + ersatzJeJahr[t];
                double einnahmen = erloesJahr;   // feste Einspeisevergütung, nominal konstant
                if (zusatzErloesReihen != null)
                    foreach (ErloesReihe reihe in zusatzErloesReihen)
                        if (reihe != null) einnahmen += reihe.Wert(t);   // KWKG + Steuern (E4)

                z.BarwertAusgaben += ausgaben * faktor;
                z.BarwertEinnahmen += einnahmen * faktor;
                z.BarwertReihe[t] = (einnahmen - ausgaben) * faktor;
                z.NominalReihe[t] = einnahmen - ausgaben;
            }

            z.RestwertNominal = restwertT;
            z.RestwertBarwert = restwertT * Math.Pow(1.0 + i, -T);
            z.Kapitalwert = -z.Investition - z.BarwertAusgaben + z.BarwertEinnahmen + z.RestwertBarwert;
            return z;
        }

        /// <summary>
        /// Interner Zinsfuß [%] der Differenzreihe Variante − Stamm (inkl. Restwert-
        /// differenz im letzten Jahr): Nullstelle von KW(r) per Bisektion in
        /// (−99 %, 1000 %). null = kein Vorzeichenwechsel (keine klassische
        /// Investitionsreihe) oder keine Konvergenz.
        /// </summary>
        public static double? InternerZinsfuss(Zahlungsbild variante, Zahlungsbild stamm)
        {
            if (variante == null || stamm == null ||
                variante.NominalReihe == null || stamm.NominalReihe == null) return null;
            int T = Math.Min(variante.NominalReihe.Length, stamm.NominalReihe.Length) - 1;

            double[] fluss = new double[T + 1];
            for (int t = 0; t <= T; t++) fluss[t] = variante.NominalReihe[t] - stamm.NominalReihe[t];
            fluss[T] += variante.RestwertNominal - stamm.RestwertNominal;

            Func<double, double> kw = r =>
            {
                double summe = 0;
                for (int t = 0; t <= T; t++) summe += fluss[t] / Math.Pow(1.0 + r, t);
                return summe;
            };

            double lo = -0.99, hi = 10.0;
            double fLo = kw(lo), fHi = kw(hi);
            if (double.IsNaN(fLo) || double.IsNaN(fHi) || fLo * fHi > 0) return null;

            for (int iter = 0; iter < 200; iter++)
            {
                double mid = (lo + hi) / 2.0, fMid = kw(mid);
                if (Math.Abs(fMid) < 1e-6 || (hi - lo) < 1e-9) return Math.Round(mid * 100.0, 2);
                if (fLo * fMid <= 0) { hi = mid; fHi = fMid; } else { lo = mid; fLo = fMid; }
            }
            return Math.Round((lo + hi) / 2.0 * 100.0, 2);
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
