namespace SpeicherEngine
{
    /// <summary>
    /// Eingangsgroessen der Wirtschaftlichkeitsrechnung.
    /// </summary>
    /// <remarks>
    /// <see cref="ErtragReferenzjahrEur"/> ist der bereits fertige Jahresertrag
    /// E_a,1 [EUR/a] - im Excel-Kompatibilitaetsmodus also die um den pauschalen
    /// Verlustfaktor gekuerzte Euro-Summe (V7-Zelle N10), im energetischen Modus
    /// die ungekuerzte Summe. Die Fallunterscheidung trifft die Strategie, nicht
    /// dieser Typ.
    /// </remarks>
    public sealed record WirtschaftlichkeitEingang
    {
        /// <summary>Ertrag des Referenzjahres E_a,1 [EUR/a] (V7: N10).</summary>
        public double ErtragReferenzjahrEur { get; init; }

        /// <summary>Investition I [EUR] (V7: N6).</summary>
        public double InvestitionEur { get; init; }

        /// <summary>Kapitalzins i_z [-] (V7: N4).</summary>
        public double Kapitalzins { get; init; }

        /// <summary>Nutzungsdauer N [a] (V7: N7).</summary>
        public double NutzungsdauerA { get; init; }

        /// <summary>Degradation d [-] pro Jahr als Bruch. 0 = ohne Degradation (V7-Verhalten).</summary>
        public double DegradationProA { get; init; }
    }

    /// <summary>
    /// Ergebnisblock der Wirtschaftlichkeitsrechnung. Die Kommentare nennen die
    /// zugehoerige Zelle der V7-Mappe, soweit es eine gibt.
    /// </summary>
    public sealed record WirtschaftlichkeitErgebnis
    {
        /// <summary>Investition I [EUR] (N6).</summary>
        public double InvestitionEur { get; init; }

        /// <summary>Ertrag des Referenzjahres E_a,1 [EUR/a] (N10).</summary>
        public double ErtragReferenzjahrEur { get; init; }

        /// <summary>Degradierter Rentenbarwertfaktor RBF_deg [a] (Fachkonzept 5.3).</summary>
        public double RbfDeg { get; init; }

        /// <summary>Annuitaetsfaktor a(i_z, N) [1/a].</summary>
        public double Annuitaetsfaktor { get; init; }

        /// <summary>
        /// Degradationsaequivalenter Jahresertrag E_a,aeq = E_a,1 * RBF_deg * a [EUR/a].
        /// Bei d = 0 identisch mit <see cref="ErtragReferenzjahrEur"/> bis auf Rundung.
        /// </summary>
        public double ErtragAequivalentEur { get; init; }

        /// <summary>Annuitaet (Kapitaldienst) A = I * a [EUR/a] (N12).</summary>
        public double AnnuitaetEur { get; init; }

        /// <summary>Jahresueberschuss nach Kapitaldienst dJ = E_a,aeq - A [EUR/a] (N13).</summary>
        public double JahresueberschussEur { get; init; }

        /// <summary>Statische Amortisation T_stat = I / E_a,aeq [a] (N15).</summary>
        public Amortisation StatischeAmortisation { get; init; }

        /// <summary>Dynamische Amortisation [a] (N16).</summary>
        public Amortisation DynamischeAmortisation { get; init; }

        /// <summary>Kapitalwert NPV = E_a,1 * RBF_deg - I [EUR] (N17).</summary>
        public double KapitalwertEur { get; init; }
    }
}
