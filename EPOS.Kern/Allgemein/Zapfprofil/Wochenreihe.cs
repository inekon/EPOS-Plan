using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was die Wochenreihe je Zone braucht (4.2, 4.7): die 365 Tagesmengen der Auslegung [kWh]
    /// (<see cref="Wochenreihe.TagesmengenAuslegung"/>), die normierte Zeitstruktur mit den
    /// Tagesgängen und der Kalender der Zone (samt Ferien).
    /// </summary>
    internal sealed record Wochenbaustein(string Zone, double[] TagesmengenKwh, Zeitstruktur Struktur,
                                          ZapfTagtyp[] Kalender);

    /// <summary>
    /// <b>Die Wochenreihe der Auslegung</b> (Umsetzungskonzept Zapfprofilgenerator 4.2, 4.7):
    /// 168 Stundenwerte [kWh] der maßgebenden Woche — gebildet aus den Tagesmengen der Zonen
    /// bei θ_KW,Auslegung (Faktor f_KW,A) mal Tagesgang, über dieselben Bausteine wie die Bilanz,
    /// aber <b>ohne</b> Stundenreihe und ohne Bilanzfassade.
    ///
    /// <code>
    /// f_KW,A      = (θ_Zapf − θ_KW,Auslegung) / (θ_Zapf − θ̄_KW)          je Zone, für alle Tage gleich
    /// Q_a,A       = Q_a · f_KW,A;  Q_d,A = Q_a,A · g_A(d) / Σ g_A,  g_A(d) = f_Monat · 7 · w_T(d)
    /// S(d)        = Σ_Zonen Q_d,A                                      Tagessumme der Gruppe
    /// maßgebend   = die sieben aufeinanderfolgenden Tage d0 … d0+6 (1 ≤ d0 ≤ 359) mit der
    ///               größten Σ S(d); bei Gleichstand die frühere Woche
    /// q(k·24 + h) = Σ_Zonen Q_{d0+k},A · φ_Tagtyp(d0+k)(h)             k = 0 … 6, h = 0 … 23
    /// Summenkontrolle: Σ_t q(t) = Σ_{k=0..6} S(d0+k)   (relativ 1e-9, 4.7)
    /// </code>
    ///
    /// <para><b>Summenkontrolle.</b> <see cref="Bilden"/> hält die Summe der 168 Stundenwerte
    /// gegen die Summe der Tagesmengen des Fensters (<see cref="FenstersummeKwh"/>); eine
    /// Abweichung (ein Tagesgang, der nicht zu 1 summiert) steht in
    /// <see cref="SummenkontrolleErfuellt"/> und als Warnung in der Speicherauslegung.</para>
    ///
    /// <para><b>Unveränderlich.</b> Die Stundenwerte gibt die Reihe nur lesend heraus.</para>
    /// </summary>
    internal sealed class Wochenreihe
    {
        /// <summary>Sieben Tage.</summary>
        internal const int TAGE = 7;

        /// <summary>168 Stunden.</summary>
        internal const int STUNDEN = TAGE * 24;

        /// <summary>Relative Toleranz der Summenkontrolle (numerische Setzung, Rundung der Summation).</summary>
        internal const double SUMMENTOLERANZ = 1e-9;

        private readonly double[] _stundenKwh;
        private readonly double[] _tageKwh;
        private readonly ZapfTagtyp[] _tagtypen;

        private Wochenreihe(double[] stundenKwh, int ersterTag, int wochentagErsterTag, ZapfTagtyp[] tagtypen,
                            double? fenstersummeKwh)
        {
            _stundenKwh = stundenKwh;
            _tagtypen = tagtypen;
            ErsterTag = ersterTag;
            WochentagErsterTag = wochentagErsterTag;
            FenstersummeKwh = fenstersummeKwh;
            _tageKwh = new double[TAGE];
            double woche = 0.0, groesster = 0.0, spitze = 0.0;
            int tagMax = 0;
            for (int k = 0; k < TAGE; k++)
            {
                double s = 0.0;
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                {
                    double q = stundenKwh[k * Zapfkalender.STUNDEN_TAG + h];
                    s += q;
                    if (q > spitze) spitze = q;
                }
                _tageKwh[k] = s;
                woche += s;
                if (s > groesster)
                {
                    groesster = s;
                    tagMax = k;
                }
            }
            WochensummeKwh = woche;
            GroessterTagKwh = groesster;
            GroessterTagIndex = tagMax;
            GroessterStundenwertKw = spitze;
            SummenkontrolleErfuellt = !fenstersummeKwh.HasValue
                || Math.Abs(woche - fenstersummeKwh.Value) <= SUMMENTOLERANZ * Math.Max(1.0, Math.Abs(fenstersummeKwh.Value));
        }

        /// <summary>Die 168 Stundenwerte [kWh je Stunde] — nur lesbar.</summary>
        internal IReadOnlyList<double> StundenKwh => Array.AsReadOnly(_stundenKwh);

        /// <summary>Jahrestag (1 … 365) des ersten Tages der Woche.</summary>
        internal int ErsterTag { get; }

        /// <summary>Wochentag des ersten Tages, Montag = 0 … Sonntag = 6.</summary>
        internal int WochentagErsterTag { get; }

        /// <summary>Tagtyp der sieben Tage (Kalender der Klimaregion).</summary>
        internal IReadOnlyList<ZapfTagtyp> Tagtypen => Array.AsReadOnly(_tagtypen);

        /// <summary>Die sieben Tagessummen [kWh].</summary>
        internal IReadOnlyList<double> TagessummenKwh => Array.AsReadOnly(_tageKwh);

        /// <summary>Die Summe der Woche [kWh].</summary>
        internal double WochensummeKwh { get; }

        /// <summary>Der größte Tag der Woche [kWh].</summary>
        internal double GroessterTagKwh { get; }

        /// <summary>Index 0 … 6 des größten Tages.</summary>
        internal int GroessterTagIndex { get; }

        /// <summary>Der größte Stundenwert [kW].</summary>
        internal double GroessterStundenwertKw { get; }

        /// <summary>
        /// Die Summe der Tagesmengen des Fensters [kWh] (<see cref="Bilden"/>); <c>null</c> bei
        /// einer Reihe aus Stundenwerten (<see cref="Aus"/>).
        /// </summary>
        internal double? FenstersummeKwh { get; }

        /// <summary>Stimmt die Wochensumme mit <see cref="FenstersummeKwh"/> (relativ 1e-9)? Ohne Fenstersumme ja.</summary>
        internal bool SummenkontrolleErfuellt { get; }

        /// <summary>Wochentag (Montag = 0) des Tages <paramref name="index"/> (0 … 6) der Woche.</summary>
        internal int Wochentag(int index) => (WochentagErsterTag + index) % TAGE;

        // =================================================================================
        // Bildung
        // =================================================================================

        /// <summary>
        /// Eine Wochenreihe aus 168 Stundenwerten; negative oder nicht endliche Werte, eine
        /// falsche Länge oder ein Wochentag außerhalb 0 … 6 werden benannt abgelehnt.
        /// </summary>
        internal static Wochenreihe Aus(double[] stundenKwh, int ersterTag, int wochentagErsterTag, ZapfTagtyp[] tagtypen)
        {
            if (stundenKwh == null || stundenKwh.Length != STUNDEN)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_WOCHE_168"));
            foreach (double q in stundenKwh)
                if (double.IsNaN(q) || double.IsInfinity(q) || q < 0)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_WOCHE_WERT"));
            if (wochentagErsterTag < 0 || wochentagErsterTag >= TAGE)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_WOCHE_WOCHENTAG"));
            if (tagtypen == null || tagtypen.Length != TAGE)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_WOCHE_TAGTYPEN"));
            return new Wochenreihe((double[])stundenKwh.Clone(), ersterTag, wochentagErsterTag, (ZapfTagtyp[])tagtypen.Clone(),
                                   null);
        }

        /// <summary>
        /// <b>Die Wochenreihe der maßgebenden Woche</b> einer Topologiegruppe (Formel oben).
        /// <paramref name="regionskalender"/> ist der Kalender der Klimaregion ohne Ferien und
        /// benennt die Tagtypen der Woche.
        /// </summary>
        internal static Wochenreihe Bilden(IReadOnlyList<Wochenbaustein> zonen, int wochentagJan1,
                                           ZapfTagtyp[] regionskalender)
        {
            Pruefen(zonen);
            if (regionskalender == null || regionskalender.Length != Zapfkalender.TAGE)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                    ZapfSatz.Neu("EINGABE_KALENDER_365"));
            double[] s = Tagessummen(zonen);

            int beste = 1;
            double besteSumme = double.NegativeInfinity;
            for (int d0 = 1; d0 <= Zapfkalender.TAGE - TAGE + 1; d0++)
            {
                double w = 0.0;
                for (int k = 0; k < TAGE; k++) w += s[d0 - 1 + k];
                if (w > besteSumme)
                {
                    besteSumme = w;
                    beste = d0;
                }
            }

            var stunden = new double[STUNDEN];
            var typen = new ZapfTagtyp[TAGE];
            for (int k = 0; k < TAGE; k++)
            {
                double[] tag = Tagesstunden(zonen, beste + k);
                Array.Copy(tag, 0, stunden, k * Zapfkalender.STUNDEN_TAG, Zapfkalender.STUNDEN_TAG);
                typen[k] = regionskalender[beste - 1 + k];
            }
            // Summenkontrolle: die 168 Stundenwerte gegen die Tagesmengen des Fensters (besteSumme).
            return new Wochenreihe(stunden, beste, Zapfkalender.Wochentag(wochentagJan1, beste), typen, besteSumme);
        }

        /// <summary>Der Jahrestag (1 … 365) mit der größten Tagessumme der Gruppe; bei Gleichstand der frühere.</summary>
        internal static int GroessterTag(IReadOnlyList<Wochenbaustein> zonen)
        {
            Pruefen(zonen);
            double[] s = Tagessummen(zonen);
            int tag = 1;
            for (int d = 2; d <= Zapfkalender.TAGE; d++)
                if (s[d - 1] > s[tag - 1]) tag = d;
            return tag;
        }

        /// <summary>
        /// Die 24 Stundenwerte [kWh] eines Jahrestags: <c>Σ_Zonen Q_d,A · φ_Tagtyp(d)(h)</c>, in
        /// der Reihenfolge der Zonen summiert.
        /// </summary>
        internal static double[] Tagesstunden(IReadOnlyList<Wochenbaustein> zonen, int tag)
        {
            Pruefen(zonen);
            if (tag < 1 || tag > Zapfkalender.TAGE) throw new ArgumentOutOfRangeException(nameof(tag));
            var stunden = new double[Zapfkalender.STUNDEN_TAG];
            foreach (Wochenbaustein z in zonen)
            {
                int t = (int)z.Kalender[tag - 1] - 1;
                double q = z.TagesmengenKwh[tag - 1];
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++) stunden[h] += q * z.Struktur.Tagesgaenge[t, h];
            }
            return stunden;
        }

        /// <summary>
        /// Der Kaltwasserfaktor der Auslegung <c>f_KW,A = (θ_Zapf − θ_KW,Auslegung) / (θ_Zapf − θ̄_KW)</c>
        /// (4.2); eine nicht positive Spreizung wird benannt abgelehnt.
        /// </summary>
        internal static double KaltwasserfaktorAuslegung(double zapfC, double kaltwasserAuslegungC, double kaltwasserMittelC)
        {
            double oben = Auslegungspruefung.Spreizung(zapfC, kaltwasserAuslegungC, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_ZAPF_AUSLEGUNG"));
            double unten = Auslegungspruefung.Spreizung(zapfC, kaltwasserMittelC, ZapfSatz.Neu("BEGRIFF_SPREIZUNG_ZAPF_MITTEL"));
            return oben / unten;
        }

        /// <summary>
        /// Die 365 Tagesmengen der Auslegung [kWh]: Jahresmenge mal f_KW,A, verteilt mit den
        /// Gewichten des Formvektors ohne den Kaltwasser-Jahresgang der Bilanz (4.2, 4.5).
        /// </summary>
        internal static double[] TagesmengenAuslegung(double jahresKwh, double kaltwasserfaktorAuslegung, Zeitstruktur s,
                                                      ZapfTagtyp[] kalender, int wochentagJan1, string zone)
        {
            var eins = new double[Zapfkalender.MONATE];
            for (int m = 0; m < Zapfkalender.MONATE; m++) eins[m] = 1.0;
            return Formvektor.Tagesmengen(jahresKwh * kaltwasserfaktorAuslegung, s, kalender, wochentagJan1, eins, zone);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static double[] Tagessummen(IReadOnlyList<Wochenbaustein> zonen)
        {
            var s = new double[Zapfkalender.TAGE];
            foreach (Wochenbaustein z in zonen)
                for (int d = 0; d < Zapfkalender.TAGE; d++) s[d] += z.TagesmengenKwh[d];
            return s;
        }

        private static void Pruefen(IReadOnlyList<Wochenbaustein> zonen)
        {
            if (zonen == null || zonen.Count == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_WOCHE_OHNE_ZONE"));
            foreach (Wochenbaustein z in zonen)
                if (z == null || z.TagesmengenKwh == null || z.TagesmengenKwh.Length != Zapfkalender.TAGE
                    || z.Kalender == null || z.Kalender.Length != Zapfkalender.TAGE || z.Struktur == null)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.WochenreiheUngueltig,
                        ZapfSatz.Neu("AUSLEGUNG_WOCHE_ZONE_365"));
        }
    }
}
