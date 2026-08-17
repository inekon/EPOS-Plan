using System.Globalization;

namespace SpeicherEngine
{
    /// <summary>
    /// Ergebniszustand einer Amortisationsrechnung. Ersetzt die Textwerte
    /// ("nicht amortisierbar", "&gt; Nutzungsdauer"), die die V7-Mappe in
    /// dieselbe Zelle schrieb wie die Zahl.
    /// </summary>
    public enum AmortisationStatus
    {
        /// <summary>Es gibt eine Amortisationszeit; sie steht in <see cref="Amortisation.Jahre"/>.</summary>
        Amortisierbar = 0,

        /// <summary>Jahresertrag kleiner oder gleich 0 - V7-Text "nicht amortisierbar".</summary>
        NichtAmortisierbar = 1,

        /// <summary>
        /// Der Barwert der Ertraege ueber die Nutzungsdauer erreicht die Investition
        /// nicht - V7-Text "&gt; Nutzungsdauer".
        /// </summary>
        UeberNutzungsdauer = 2
    }

    /// <summary>
    /// Ergebnis einer statischen oder dynamischen Amortisationsrechnung.
    /// Unveraenderlicher Werttyp.
    /// </summary>
    public readonly record struct Amortisation
    {
        private Amortisation(AmortisationStatus status, double jahre)
        {
            Status = status;
            Jahre = jahre;
        }

        /// <summary>Zustand der Rechnung.</summary>
        public AmortisationStatus Status { get; }

        /// <summary>
        /// Amortisationszeit [a]; <c>double.PositiveInfinity</c>, wenn
        /// <see cref="Status"/> nicht <see cref="AmortisationStatus.Amortisierbar"/> ist.
        /// </summary>
        public double Jahre { get; }

        /// <summary>True, wenn eine endliche Amortisationszeit vorliegt.</summary>
        public bool IstAmortisierbar => Status == AmortisationStatus.Amortisierbar;

        /// <summary>Erzeugt ein amortisierbares Ergebnis mit der angegebenen Dauer [a].</summary>
        public static Amortisation Jahreswert(double jahre)
            => new Amortisation(AmortisationStatus.Amortisierbar, jahre);

        /// <summary>V7-Fall "nicht amortisierbar".</summary>
        public static Amortisation NichtAmortisierbar
            => new Amortisation(AmortisationStatus.NichtAmortisierbar, double.PositiveInfinity);

        /// <summary>V7-Fall "&gt; Nutzungsdauer".</summary>
        public static Amortisation UeberNutzungsdauer
            => new Amortisation(AmortisationStatus.UeberNutzungsdauer, double.PositiveInfinity);

        /// <summary>
        /// Zeichenkette in der Schreibweise der V7-Mappe ("nicht amortisierbar",
        /// "&gt; Nutzungsdauer", sonst die Zahl invariant formatiert). Nur fuer
        /// Diagnose und Tests - die UI formatiert selbst kulturabhaengig.
        /// </summary>
        public override string ToString()
        {
            switch (Status)
            {
                case AmortisationStatus.NichtAmortisierbar: return "nicht amortisierbar";
                case AmortisationStatus.UeberNutzungsdauer: return "> Nutzungsdauer";
                default: return Jahre.ToString("R", CultureInfo.InvariantCulture);
            }
        }
    }
}
