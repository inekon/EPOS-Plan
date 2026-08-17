using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Wirtschaftlichkeitsrechnung des Speichermoduls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die statischen Einzelfunktionen sind zeichengetreue Portierungen der
    /// Blattformeln der V7-Mappe (ueber <c>speicher_sim.py</c>): gleiche
    /// Klammerung, gleiche Reihenfolge der Sonderfallpruefungen. Sie sind die
    /// Grundlage der Referenztests.
    /// </para>
    /// <para>
    /// <see cref="Berechne"/> setzt sie zum vollstaendigen Block zusammen und
    /// ergaenzt die Degradation nach Fachkonzept 5.3. Bei d = 0 faellt die
    /// Rechnung auf das V7-Verhalten zurueck.
    /// </para>
    /// <para>Die Klasse ist zustandslos und damit thread-sicher.</para>
    /// </remarks>
    public static class Wirtschaftlichkeit
    {
        /// <summary>
        /// Annuitaetsfaktor a(i_z, N) = i_z / (1 - (1 + i_z)^-N) [1/a];
        /// bei i_z = 0 gilt a = 1/N.
        /// </summary>
        public static double Annuitaetsfaktor(double zins, double nutzungsdauerA)
        {
            if (zins == 0.0) return 1.0 / nutzungsdauerA;
            return zins / (1.0 - Math.Pow(1.0 + zins, -nutzungsdauerA));
        }

        /// <summary>
        /// Annuitaet (Kapitaldienst) A [EUR/a]. Portierung von
        /// <c>N12 = IF(N4=0; N6/N7; N6*N4/(1-(1+N4)^-N7))</c>.
        /// </summary>
        public static double Annuitaet(double investEur, double zins, double nutzungsdauerA)
        {
            if (zins == 0.0) return investEur / nutzungsdauerA;
            return investEur * zins / (1.0 - Math.Pow(1.0 + zins, -nutzungsdauerA));
        }

        /// <summary>
        /// Gewoehnlicher Rentenbarwertfaktor RBF = (1 - (1 + i_z)^-N) / i_z [a];
        /// bei i_z = 0 gilt RBF = N.
        /// </summary>
        public static double Rentenbarwertfaktor(double zins, double nutzungsdauerA)
        {
            if (zins == 0.0) return nutzungsdauerA;
            return (1.0 - Math.Pow(1.0 + zins, -nutzungsdauerA)) / zins;
        }

        /// <summary>
        /// Degradierter Rentenbarwertfaktor RBF_deg [a] nach Fachkonzept 5.3:
        /// <code>
        /// q       = (1 - d) / (1 + i_z)
        /// RBF_deg = (1 / (1 + i_z)) * (1 - q^N) / (1 - q)
        /// </code>
        /// Grenzfaelle in dieser Pruefreihenfolge:
        /// d = 0 und i_z = 0 -&gt; N; d = 0 -&gt; gewoehnlicher Rentenbarwertfaktor;
        /// i_z = 0 -&gt; (1 - (1 - d)^N) / d.
        /// </summary>
        /// <param name="degradationProA">Degradation d [-] pro Jahr als Bruch.</param>
        /// <param name="zins">Kapitalzins i_z [-].</param>
        /// <param name="nutzungsdauerA">Nutzungsdauer N [a].</param>
        public static double RbfDeg(double degradationProA, double zins, double nutzungsdauerA)
        {
            if (degradationProA == 0.0 && zins == 0.0) return nutzungsdauerA;
            if (degradationProA == 0.0) return Rentenbarwertfaktor(zins, nutzungsdauerA);
            if (zins == 0.0)
                return (1.0 - Math.Pow(1.0 - degradationProA, nutzungsdauerA)) / degradationProA;

            double q = (1.0 - degradationProA) / (1.0 + zins);
            return (1.0 / (1.0 + zins)) * (1.0 - Math.Pow(q, nutzungsdauerA)) / (1.0 - q);
        }

        /// <summary>
        /// Kapitalwert (NPV) [EUR] ohne Degradation. Portierung von
        /// <c>N17 = IF(N4=0; N10*N7-N6; N10*(1-(1+N4)^-N7)/N4 - N6)</c>.
        /// </summary>
        public static double Kapitalwert(double ertragEurProA, double investEur,
                                         double zins, double nutzungsdauerA)
        {
            if (zins == 0.0) return ertragEurProA * nutzungsdauerA - investEur;
            return ertragEurProA * (1.0 - Math.Pow(1.0 + zins, -nutzungsdauerA)) / zins - investEur;
        }

        /// <summary>
        /// Statische Amortisation [a]. Portierung von
        /// <c>N15 = IF(N10&lt;=0; "nicht amortisierbar"; N6/N10)</c>.
        /// </summary>
        public static Amortisation StatischeAmortisation(double ertragEurProA, double investEur)
        {
            if (ertragEurProA <= 0.0) return Amortisation.NichtAmortisierbar;
            return Amortisation.Jahreswert(investEur / ertragEurProA);
        }

        /// <summary>
        /// Dynamische Amortisation [a]. Portierung von
        /// <c>N16 = IF(N10&lt;=0; "nicht amortisierbar";
        ///       IF(N4=0; N6/N10;
        ///       IF(N10*((1-(1+N4)^-N7)/N4)&lt;N6; "&gt; Nutzungsdauer";
        ///       -LN(1-N6*N4/N10)/LN(1+N4))))</c>.
        /// </summary>
        public static Amortisation DynamischeAmortisation(double ertragEurProA, double investEur,
                                                          double zins, double nutzungsdauerA)
        {
            if (ertragEurProA <= 0.0) return Amortisation.NichtAmortisierbar;
            if (zins == 0.0) return Amortisation.Jahreswert(investEur / ertragEurProA);
            if (ertragEurProA * ((1.0 - Math.Pow(1.0 + zins, -nutzungsdauerA)) / zins) < investEur)
                return Amortisation.UeberNutzungsdauer;
            return Amortisation.Jahreswert(
                -Math.Log(1.0 - investEur * zins / ertragEurProA) / Math.Log(1.0 + zins));
        }

        /// <summary>
        /// Rechnet den vollstaendigen Wirtschaftlichkeitsblock (Fachkonzept 6.2, 5.3).
        /// </summary>
        /// <remarks>
        /// <code>
        /// a        = i_z / (1 - (1 + i_z)^-N)
        /// A        = I * a                          (V7: N12)
        /// RBF_deg  = siehe <see cref="RbfDeg"/>
        /// E_a,aeq  = E_a,1 * RBF_deg * a            (V7: identisch E_a,1 bei d = 0)
        /// dJ       = E_a,aeq - A                    (V7: N13)
        /// T_stat   = I / E_a,aeq                    (V7: N15)
        /// T_dyn    = siehe <see cref="DynamischeAmortisation"/>  (V7: N16)
        /// NPV      = E_a,1 * RBF_deg - I            (V7: N17)
        /// </code>
        /// Bei d = 0 stimmen alle Groessen mit den V7-Blattformeln bis auf die
        /// Assoziativitaet der Multiplikationen ueberein (relative Abweichung
        /// im Bereich weniger ULP, weit unterhalb der geforderten 1e-12).
        /// </remarks>
        public static WirtschaftlichkeitErgebnis Berechne(WirtschaftlichkeitEingang eingang)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));

            double e1 = eingang.ErtragReferenzjahrEur;
            double invest = eingang.InvestitionEur;
            double zins = eingang.Kapitalzins;
            double nutzungsdauer = eingang.NutzungsdauerA;
            double d = eingang.DegradationProA;

            double rbfDeg = RbfDeg(d, zins, nutzungsdauer);
            double annuitaetsfaktor = Annuitaetsfaktor(zins, nutzungsdauer);
            double ertragAequivalent = e1 * rbfDeg * annuitaetsfaktor;
            double annuitaet = Annuitaet(invest, zins, nutzungsdauer);

            return new WirtschaftlichkeitErgebnis
            {
                InvestitionEur = invest,
                ErtragReferenzjahrEur = e1,
                RbfDeg = rbfDeg,
                Annuitaetsfaktor = annuitaetsfaktor,
                ErtragAequivalentEur = ertragAequivalent,
                AnnuitaetEur = annuitaet,
                JahresueberschussEur = ertragAequivalent - annuitaet,
                StatischeAmortisation = StatischeAmortisation(ertragAequivalent, invest),
                DynamischeAmortisation = DynamischeAmortisation(ertragAequivalent, invest, zins, nutzungsdauer),
                KapitalwertEur = e1 * rbfDeg - invest
            };
        }
    }
}
