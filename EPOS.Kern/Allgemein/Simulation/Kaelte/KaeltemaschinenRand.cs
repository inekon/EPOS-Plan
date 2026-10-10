using System;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Kennfeldpunkt der Kältemaschine nach Randweg: Leistung, EER und die Kennzeichen der Stunde.</summary>
    public readonly struct KaeltemaschinenRandpunkt
    {
        /// <summary>Verfügbare Kälteleistung Q_av [kW]; außerhalb des Kennfelds der Randwert.</summary>
        public readonly double LeistungKw;

        /// <summary>Volllast-EER der Stunde.</summary>
        public readonly double Eer;

        /// <summary><c>true</c> = mindestens eine Achse lag außerhalb der Stützstellen (Randstunde).</summary>
        public readonly bool Randwert;

        /// <summary><c>true</c> = der EER wurde mit dem Gütegrad über den Rand fortgesetzt (zusätzlich zu <see cref="Randwert"/>).</summary>
        public readonly bool Extrapoliert;

        /// <summary>Konstruktor.</summary>
        public KaeltemaschinenRandpunkt(double leistungKw, double eer, bool randwert, bool extrapoliert)
        {
            LeistungKw = leistungKw;
            Eer = eer;
            Randwert = randwert;
            Extrapoliert = extrapoliert;
        }
    }

    /// <summary>
    /// <b>Die Ränder des Kennfelds der Kältemaschine</b> (KM3, Fachkonzept Teillast und Takten 3.4). Ohne Datenbank,
    /// ohne Dienste, deterministisch.
    ///
    /// <para><b>RANDWERT</b> (Vorgabe): außerhalb der Stützstellen gilt der Randwert der Kennlinie — das heutige
    /// Verhalten von <see cref="KaeltemaschinenKennlinie.Auswerten"/>.</para>
    ///
    /// <para><b>GUETEGRAD</b> (Hausmodell nach dem Muster der EN 15316-4-2): Der Carnot-EER einer Stunde ist die
    /// Kaltwassertemperatur in Kelvin geteilt durch den Hub zwischen Rückkühl- und Kaltwassertemperatur, mindestens
    /// <see cref="MINDESTHUB_K"/>. Der Gütegrad am Randpunkt ist der Rand-EER durch den Carnot-EER des Randpunkts; der
    /// Randpunkt ist die Auswertung bei den auf das Kennfeld geklemmten Temperaturen. Außerhalb gilt EER = Gütegrad mal
    /// Carnot-EER der Stunde, je Achse höchstens <see cref="EXTRAPOLATIONSWEITE_K"/> über den Rand hinaus und gedeckelt
    /// auf <see cref="KaelteFestwerte.FREIE_KUEHLUNG_EER"/>. Die Leistung bleibt am Randwert.</para>
    /// </summary>
    public static class KaeltemaschinenRand
    {
        /// <summary>Nullpunkt der Celsius-Skala [K].</summary>
        public const double KELVIN = 273.15;

        /// <summary>Mindesthub ΔT_min des Carnot-EER [K] (Fachkonzept 3.4, Vorgabe).</summary>
        public const double MINDESTHUB_K = 5.0;

        /// <summary>Größte Extrapolationsweite je Achse über den Kennfeldrand [K] (Fachkonzept 3.4, Vorgabe).</summary>
        public const double EXTRAPOLATIONSWEITE_K = 10.0;

        /// <summary>Rechnet der Randweg mit dem Gütegrad? Nur bei <c>GUETEGRAD</c>; leer oder sonst gilt der Randwert.</summary>
        public static bool IstGuetegrad(string randweg)
            => !string.IsNullOrWhiteSpace(randweg)
               && randweg.Trim().ToUpperInvariant() == KaeltemaschineTeillastSchema.RANDWEG_GUETEGRAD;

        /// <summary>Der Carnot-EER (T_kw + 273,15) / max(T_rk − T_kw, ΔT_min), Temperaturen in °C.</summary>
        public static double CarnotEer(double kaltwasserC, double rueckkuehlC)
            => (kaltwasserC + KELVIN) / Math.Max(rueckkuehlC - kaltwasserC, MINDESTHUB_K);

        /// <summary>Der Gütegrad η_R = EER_Rand / EER_C(Randpunkt).</summary>
        public static double Guetegrad(double eerRand, double kaltwasserRandC, double rueckkuehlRandC)
            => eerRand / CarnotEer(kaltwasserRandC, rueckkuehlRandC);

        /// <summary>
        /// Der mit dem Gütegrad fortgesetzte EER bei (T_kw, T_rk) aus dem Randpunkt (EER_Rand bei T_kw,Rand und T_rk,Rand):
        /// η_R mal Carnot-EER der Stunde, je Achse höchstens <see cref="EXTRAPOLATIONSWEITE_K"/> über den Rand, gedeckelt auf
        /// <see cref="KaelteFestwerte.FREIE_KUEHLUNG_EER"/>. Am Randpunkt selbst gleich EER_Rand.
        /// </summary>
        public static double EerFortgesetzt(double eerRand, double kaltwasserRandC, double rueckkuehlRandC,
                                            double kaltwasserC, double rueckkuehlC)
        {
            if (!(eerRand > 0)) return eerRand;
            double kw = kaltwasserRandC + Begrenzt(kaltwasserC - kaltwasserRandC);
            double rk = rueckkuehlRandC + Begrenzt(rueckkuehlC - rueckkuehlRandC);
            double eer = Guetegrad(eerRand, kaltwasserRandC, rueckkuehlRandC) * CarnotEer(kw, rk);
            return Math.Min(eer, Math.Max(eerRand, KaelteFestwerte.FREIE_KUEHLUNG_EER));
        }

        /// <summary>
        /// Wertet die Kennlinie bei (T_rk, T_kw) nach dem Randweg aus: RANDWERT = <see cref="KaeltemaschinenKennlinie.Auswerten"/>
        /// unverändert; GUETEGRAD = in einer Randstunde der EER nach <see cref="EerFortgesetzt"/> am geklemmten Randpunkt,
        /// gekennzeichnet als <see cref="KaeltemaschinenRandpunkt.Extrapoliert"/>, die Leistung am Randwert.
        /// </summary>
        public static KaeltemaschinenRandpunkt Auswerten(KaeltemaschinenKennlinie kennlinie, double rueckkuehlC,
                                                         double kaltwasserC, string randweg)
        {
            if (kennlinie == null || kennlinie.Leer) return default;
            KaeltemaschinenPunkt p = kennlinie.Auswerten(rueckkuehlC, kaltwasserC);
            if (!p.Randwert || !IstGuetegrad(randweg) || !(p.Eer > 0))
                return new KaeltemaschinenRandpunkt(p.LeistungKw, p.Eer, p.Randwert, false);

            double kwMin = kennlinie.Kaltwasserstuetzstellen.First();
            double kwMax = kennlinie.Kaltwasserstuetzstellen.Last();
            double kwRand = Math.Max(kwMin, Math.Min(kwMax, kaltwasserC));
            double rkRand = Math.Max(kennlinie.RueckkuehlMin, Math.Min(kennlinie.RueckkuehlMax, rueckkuehlC));
            bool ausserhalb = kwRand != kaltwasserC || rkRand != rueckkuehlC;
            if (!ausserhalb)
                return new KaeltemaschinenRandpunkt(p.LeistungKw, p.Eer, true, false);
            double eer = EerFortgesetzt(p.Eer, kwRand, rkRand, kaltwasserC, rueckkuehlC);
            return new KaeltemaschinenRandpunkt(p.LeistungKw, eer, true, true);
        }

        private static double Begrenzt(double d) => Math.Max(-EXTRAPOLATIONSWEITE_K, Math.Min(EXTRAPOLATIONSWEITE_K, d));
    }
}
