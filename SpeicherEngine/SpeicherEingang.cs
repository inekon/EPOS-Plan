using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Eingangszeitreihen einer Speichersimulation. Alle Reihen sind gleich lang;
    /// die Laenge n ist beliebig (Referenztest 35.137, produktiv 35.040 bzw.
    /// 35.136 Viertelstunden).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Konstruktoren kopieren die uebergebenen Arrays. Eine Instanz ist damit
    /// nach der Konstruktion unveraenderlich und kann in der Rastersuche gefahrlos
    /// von mehreren Threads gelesen werden (Fachkonzept 8.1). Die veroeffentlichten
    /// Arrays sind die internen Kopien - sie duerfen vom Aufrufer nicht veraendert
    /// werden.
    /// </para>
    /// <para>
    /// <b>Optionale Reihen</b> (AP2): BHKW-Erzeugung und die getrennten
    /// Verguetungsreihen v_pv / v_bhkw. <c>null</c> bedeutet jeweils "nicht
    /// vorhanden": ohne BHKW-Reihe rechnet die Engine mit P_bhkw = 0, ohne
    /// Verguetungsreihe mit dem Standardwert
    /// <see cref="SpeicherParameter.VerguetungCtKwh"/>. Ein Eingang aus Stufe 1
    /// verhaelt sich dadurch unveraendert.
    /// </para>
    /// </remarks>
    public sealed class SpeicherEingang
    {
        /// <summary>Lastgang [kW] je Intervall (V7-Blattspalte B).</summary>
        public double[] LastKw { get; }

        /// <summary>Erzeugung / Quellenleistung [kW] je Intervall (V7-Blattspalte C).</summary>
        public double[] PvKw { get; }

        /// <summary>Bezugspreis [ct/kWh] je Intervall (V7-Blattspalte D).</summary>
        public double[] PreisCtKwh { get; }

        /// <summary>
        /// BHKW-Erzeugung [kW] je Intervall, auf das Viertelstundenraster expandiert
        /// (Fachkonzept 3.3). <c>null</c> = kein BHKW im Projekt.
        /// </summary>
        public double[]? BhkwKw { get; }

        /// <summary>
        /// Einspeiseverguetung v_pv [ct/kWh] je Intervall. <c>null</c> = Standardwert
        /// aus den Parametern.
        /// </summary>
        public double[]? VerguetungPvCtKwh { get; }

        /// <summary>
        /// Einspeise-/KWK-Erloes v_bhkw [ct/kWh] je Intervall. <c>null</c> =
        /// Standardwert aus den Parametern.
        /// </summary>
        public double[]? VerguetungBhkwCtKwh { get; }

        /// <summary>Anzahl der Intervalle n.</summary>
        public int Anzahl => LastKw.Length;

        /// <summary>true, wenn eine BHKW-Reihe hinterlegt ist.</summary>
        public bool HatBhkwReihe => BhkwKw != null;

        /// <summary>
        /// Erzeugt einen Eingang aus den Pflichtreihen und den optionalen Reihen.
        /// </summary>
        /// <param name="lastKw">Lastgang [kW], Pflicht.</param>
        /// <param name="pvKw">PV-Erzeugung [kW], Pflicht.</param>
        /// <param name="preisCtKwh">Bezugspreis [ct/kWh], Pflicht.</param>
        /// <param name="bhkwKw">BHKW-Erzeugung [kW], optional.</param>
        /// <param name="verguetungPvCtKwh">v_pv [ct/kWh], optional.</param>
        /// <param name="verguetungBhkwCtKwh">v_bhkw [ct/kWh], optional.</param>
        /// <exception cref="ArgumentNullException">Wenn eine Pflichtreihe <c>null</c> ist.</exception>
        /// <exception cref="ArgumentException">Wenn die Reihen unterschiedlich lang oder leer sind.</exception>
        public SpeicherEingang(
            double[] lastKw,
            double[] pvKw,
            double[] preisCtKwh,
            double[]? bhkwKw = null,
            double[]? verguetungPvCtKwh = null,
            double[]? verguetungBhkwCtKwh = null)
        {
            if (lastKw == null) throw new ArgumentNullException(nameof(lastKw));
            if (pvKw == null) throw new ArgumentNullException(nameof(pvKw));
            if (preisCtKwh == null) throw new ArgumentNullException(nameof(preisCtKwh));
            if (lastKw.Length == 0)
                throw new ArgumentException("Die Zeitreihen duerfen nicht leer sein.", nameof(lastKw));
            if (pvKw.Length != lastKw.Length)
                throw new ArgumentException("PV-Reihe und Lastgang muessen gleich lang sein.", nameof(pvKw));
            if (preisCtKwh.Length != lastKw.Length)
                throw new ArgumentException("Preisreihe und Lastgang muessen gleich lang sein.", nameof(preisCtKwh));
            if (bhkwKw != null && bhkwKw.Length != lastKw.Length)
                throw new ArgumentException("BHKW-Reihe und Lastgang muessen gleich lang sein.", nameof(bhkwKw));
            if (verguetungPvCtKwh != null && verguetungPvCtKwh.Length != lastKw.Length)
                throw new ArgumentException("Verguetungsreihe v_pv und Lastgang muessen gleich lang sein.", nameof(verguetungPvCtKwh));
            if (verguetungBhkwCtKwh != null && verguetungBhkwCtKwh.Length != lastKw.Length)
                throw new ArgumentException("Verguetungsreihe v_bhkw und Lastgang muessen gleich lang sein.", nameof(verguetungBhkwCtKwh));

            LastKw = (double[])lastKw.Clone();
            PvKw = (double[])pvKw.Clone();
            PreisCtKwh = (double[])preisCtKwh.Clone();
            BhkwKw = bhkwKw == null ? null : (double[])bhkwKw.Clone();
            VerguetungPvCtKwh = verguetungPvCtKwh == null ? null : (double[])verguetungPvCtKwh.Clone();
            VerguetungBhkwCtKwh = verguetungBhkwCtKwh == null ? null : (double[])verguetungBhkwCtKwh.Clone();
        }

        /// <summary>
        /// Erzeugt einen Eingang mit konstantem Bezugspreis (Fixpreisfall, Stufe 1).
        /// </summary>
        public static SpeicherEingang MitFixpreis(double[] lastKw, double[] pvKw, double preisCtKwh)
        {
            if (lastKw == null) throw new ArgumentNullException(nameof(lastKw));
            return new SpeicherEingang(lastKw, pvKw, KonstanteReihe(preisCtKwh, lastKw.Length));
        }

        /// <summary>Liefert eine konstant befuellte Reihe der Laenge <paramref name="n"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Bei n &lt; 0.</exception>
        public static double[] KonstanteReihe(double wert, int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            double[] reihe = new double[n];
            for (int i = 0; i < n; i++) reihe[i] = wert;
            return reihe;
        }

        /// <summary>
        /// Liefert eine Kopie dieses Eingangs mit hinterlegter BHKW-Reihe.
        /// <c>null</c> entfernt sie wieder.
        /// </summary>
        public SpeicherEingang MitBhkw(double[]? bhkwKw)
            => new SpeicherEingang(LastKw, PvKw, PreisCtKwh, bhkwKw, VerguetungPvCtKwh, VerguetungBhkwCtKwh);

        /// <summary>
        /// Liefert eine Kopie dieses Eingangs mit getrennten Verguetungsreihen.
        /// </summary>
        public SpeicherEingang MitVerguetungen(double[]? verguetungPvCtKwh, double[]? verguetungBhkwCtKwh)
            => new SpeicherEingang(LastKw, PvKw, PreisCtKwh, BhkwKw, verguetungPvCtKwh, verguetungBhkwCtKwh);

        /// <summary>
        /// Komfortweg fuer den Regelfall: beide Verguetungen konstant ueber das Jahr
        /// (Fachkonzept 4.3; zeitvariable Regime folgen mit AP4).
        /// </summary>
        public SpeicherEingang MitVerguetungen(double verguetungPvCtKwh, double verguetungBhkwCtKwh)
            => MitVerguetungen(KonstanteReihe(verguetungPvCtKwh, Anzahl),
                               KonstanteReihe(verguetungBhkwCtKwh, Anzahl));

        /// <summary>
        /// Liefert einen Ausschnitt <c>[start .. start+laenge)</c> als neuen Eingang.
        /// Wird u. a. gebraucht, um den Kompatibilitaetsmodus (der Index 0 auslaesst)
        /// mit dem energetischen Modus (der ab Index 0 rechnet) zu vergleichen.
        /// </summary>
        public SpeicherEingang Ausschnitt(int start, int laenge)
        {
            if (start < 0 || start >= Anzahl)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (laenge <= 0 || start + laenge > Anzahl)
                throw new ArgumentOutOfRangeException(nameof(laenge));

            double[] last = new double[laenge];
            double[] pv = new double[laenge];
            double[] preis = new double[laenge];
            Array.Copy(LastKw, start, last, 0, laenge);
            Array.Copy(PvKw, start, pv, 0, laenge);
            Array.Copy(PreisCtKwh, start, preis, 0, laenge);

            return new SpeicherEingang(last, pv, preis,
                                       Teilstueck(BhkwKw, start, laenge),
                                       Teilstueck(VerguetungPvCtKwh, start, laenge),
                                       Teilstueck(VerguetungBhkwCtKwh, start, laenge));
        }

        private static double[]? Teilstueck(double[]? reihe, int start, int laenge)
        {
            if (reihe == null) return null;
            double[] ziel = new double[laenge];
            Array.Copy(reihe, start, ziel, 0, laenge);
            return ziel;
        }
    }
}
