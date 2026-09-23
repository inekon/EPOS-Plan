using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der mittlere Tagesgang je Tagtyp EINES Monats (Umsetzungskonzept Zapfprofilgenerator 5.1,
    /// 5.6; Reiter „Tagesgang" der Vorschau): je Tagtyp das Mittel der Stundenwerte über die Tage
    /// dieses Typs im Monat, dazu das Mittel der Zirkulation über alle Tage des Monats. Werte in
    /// kWh je Stunde = kW. Ein Tagtyp ohne Tag im Monat bleibt <c>null</c> — kein vorbelegtes
    /// Nullprofil.
    /// </summary>
    internal sealed record Tagesgangmittel
    {
        /// <summary>Der Monat 1 … 12.</summary>
        public int Monat { get; init; }

        /// <summary>24 Stundenmittel der Zapfung an Werktagen [kW]; <c>null</c> ohne Werktag im Monat.</summary>
        public IReadOnlyList<double> WerktagKw { get; init; }

        /// <summary>24 Stundenmittel der Zapfung an Samstagen [kW]; <c>null</c> ohne Samstag im Monat.</summary>
        public IReadOnlyList<double> SamstagKw { get; init; }

        /// <summary>24 Stundenmittel der Zapfung an Sonn- und Feiertagen [kW]; <c>null</c> ohne solchen Tag.</summary>
        public IReadOnlyList<double> SonnFeiertagKw { get; init; }

        /// <summary>Wie viele Tage je Tagtyp in das Mittel eingehen (Werktag, Samstag, Sonn-/Feiertag).</summary>
        public IReadOnlyList<int> TageJeTagtyp { get; init; } = new int[3];

        /// <summary>24 Stundenmittel der Zirkulation über alle Tage des Monats [kW].</summary>
        public IReadOnlyList<double> ZirkulationKw { get; init; }
    }

    /// <summary>
    /// Ein Wochenausschnitt von 168 Stunden (Konzept 5.1, 5.6; Reiter „Wochenprofil"): die
    /// Kalenderwoche Montag bis Sonntag mit dem größten Tagesbedarf der Zapfung, am Jahresrand in
    /// das Jahr hineingeschoben. Werte in kWh je Stunde = kW.
    /// </summary>
    internal sealed record Wochenausschnitt
    {
        /// <summary>Der Jahrestag 1 … 359 der ersten Stunde des Ausschnitts.</summary>
        public int Starttag { get; init; }

        /// <summary>Der Wochentag des Starttags (Montag = 0 … Sonntag = 6).</summary>
        public int WochentagStarttag { get; init; }

        /// <summary>Der Jahrestag mit dem größten Tagesbedarf der Zapfung, der die Woche bestimmt.</summary>
        public int GroessterTag { get; init; }

        /// <summary>168 Stundenwerte der Zapfung [kW].</summary>
        public IReadOnlyList<double> ZapfungKw { get; init; }

        /// <summary>168 Stundenwerte der Zirkulation [kW].</summary>
        public IReadOnlyList<double> ZirkulationKw { get; init; }
    }

    /// <summary>
    /// <b>Die Auswertung der Bilanzreihen für die Vorschau</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.1, 5.6; Stufe Z1, Gruppe 3): größter Monat, mittlerer Tagesgang je
    /// Tagtyp, Woche mit dem größten Tagesbedarf. Sie liest nur fertige
    /// <see cref="Bilanzreihe"/>n und einen Kalender — sie rechnet keinen Bedarf und ändert keine
    /// Reihe. Die Vorschau zeigt damit dieselbe Reihe, die der Lauf verbucht (2.4).
    ///
    /// <para><b>Regeln.</b> Gleichstände entscheidet der früheste Monat bzw. Tag (feste
    /// Reihenfolge, wiederholbar). Ruhetage (Ferien einer Zone) gehen in keinen der drei
    /// Tagesgänge ein. Ein Jahr ohne Zapfung nennt Januar bzw. Tag 1.</para>
    /// </summary>
    internal static class Zapfauswertung
    {
        /// <summary>Tage einer Woche.</summary>
        private const int WOCHE_TAGE = Zapfkalender.WOCHENTAGE;

        /// <summary>Der Monat 1 … 12 mit der größten Monatssumme der Reihe; bei Gleichstand der früheste.</summary>
        internal static int GroessterMonat(Bilanzreihe reihe)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));
            IReadOnlyList<double> m = reihe.MonatssummenKwh;
            int best = 0;
            for (int i = 1; i < m.Count; i++) if (m[i] > m[best]) best = i;
            return best + 1;
        }

        /// <summary>Der Jahrestag 1 … 365 mit der größten Tagessumme der Reihe; bei Gleichstand der früheste.</summary>
        internal static int GroessterTag(Bilanzreihe reihe)
        {
            if (reihe == null) throw new ArgumentNullException(nameof(reihe));
            IReadOnlyList<double> h = reihe.StundenKwh;
            int best = 1;
            double bestKwh = double.NegativeInfinity;
            for (int d = 1; d <= Zapfkalender.TAGE; d++)
            {
                double tagKwh = 0.0;
                int von = (d - 1) * Zapfkalender.STUNDEN_TAG;
                for (int s = 0; s < Zapfkalender.STUNDEN_TAG; s++) tagKwh += h[von + s];
                if (tagKwh > bestKwh) { bestKwh = tagKwh; best = d; }
            }
            return best;
        }

        /// <summary>
        /// Der mittlere Tagesgang je Tagtyp im Monat <paramref name="monat"/>. Der Kalender trägt
        /// 365 Tagtypen (<see cref="Zapfkalender.Bilden"/>); Ruhetage zählen zu keinem Tagtyp.
        /// </summary>
        internal static Tagesgangmittel Tagesgang(Bilanzreihe zapfung, Bilanzreihe zirkulation, int monat,
                                                  IReadOnlyList<ZapfTagtyp> kalender)
        {
            if (zapfung == null) throw new ArgumentNullException(nameof(zapfung));
            if (zirkulation == null) throw new ArgumentNullException(nameof(zirkulation));
            if (kalender == null || kalender.Count != Zapfkalender.TAGE)
                throw new ArgumentException("Der Kalender trägt nicht 365 Tagtypen.", nameof(kalender));
            if (monat < 1 || monat > Zapfkalender.MONATE) throw new ArgumentOutOfRangeException(nameof(monat));

            int ersterTag = 1;
            for (int m = 0; m < monat - 1; m++) ersterTag += Zapfkalender.TageJeMonat[m];
            int tage = Zapfkalender.TageJeMonat[monat - 1];

            var summen = new double[3, Zapfkalender.STUNDEN_TAG];
            var zaehler = new int[3];
            var zirk = new double[Zapfkalender.STUNDEN_TAG];
            IReadOnlyList<double> zapf = zapfung.StundenKwh;
            IReadOnlyList<double> umlauf = zirkulation.StundenKwh;

            for (int d = ersterTag; d < ersterTag + tage; d++)
            {
                int von = (d - 1) * Zapfkalender.STUNDEN_TAG;
                for (int s = 0; s < Zapfkalender.STUNDEN_TAG; s++) zirk[s] += umlauf[von + s];

                int t = Index(kalender[d - 1]);
                if (t < 0) continue;
                zaehler[t]++;
                for (int s = 0; s < Zapfkalender.STUNDEN_TAG; s++) summen[t, s] += zapf[von + s];
            }

            for (int s = 0; s < Zapfkalender.STUNDEN_TAG; s++) zirk[s] /= tage;

            return new Tagesgangmittel
            {
                Monat = monat,
                WerktagKw = Mittel(summen, zaehler, 0),
                SamstagKw = Mittel(summen, zaehler, 1),
                SonnFeiertagKw = Mittel(summen, zaehler, 2),
                TageJeTagtyp = Array.AsReadOnly(zaehler),
                ZirkulationKw = Array.AsReadOnly(zirk)
            };
        }

        /// <summary>
        /// Die Woche Montag bis Sonntag, die den Tag mit dem größten Tagesbedarf der Zapfung
        /// enthält (<see cref="GroessterTag"/>); läge sie teils vor dem 1. Januar oder nach dem
        /// 31. Dezember, wird sie in das Jahr geschoben (Beginn 1 bzw. 359).
        /// </summary>
        internal static Wochenausschnitt Woche(Bilanzreihe zapfung, Bilanzreihe zirkulation, int wochentagJan1)
        {
            if (zapfung == null) throw new ArgumentNullException(nameof(zapfung));
            if (zirkulation == null) throw new ArgumentNullException(nameof(zirkulation));
            if (wochentagJan1 < 0 || wochentagJan1 >= WOCHE_TAGE) throw new ArgumentOutOfRangeException(nameof(wochentagJan1));

            int groesster = GroessterTag(zapfung);
            int montag = groesster - Zapfkalender.Wochentag(wochentagJan1, groesster);
            int start = Math.Max(1, Math.Min(montag, Zapfkalender.TAGE - WOCHE_TAGE + 1));

            int stunden = WOCHE_TAGE * Zapfkalender.STUNDEN_TAG;
            var zapf = new double[stunden];
            var zirk = new double[stunden];
            int von = (start - 1) * Zapfkalender.STUNDEN_TAG;
            for (int h = 0; h < stunden; h++)
            {
                zapf[h] = zapfung.StundenKwh[von + h];
                zirk[h] = zirkulation.StundenKwh[von + h];
            }

            return new Wochenausschnitt
            {
                Starttag = start,
                WochentagStarttag = Zapfkalender.Wochentag(wochentagJan1, start),
                GroessterTag = groesster,
                ZapfungKw = Array.AsReadOnly(zapf),
                ZirkulationKw = Array.AsReadOnly(zirk)
            };
        }

        /// <summary>
        /// Der Kalender der SUMME mehrerer Zonen für den Tagesgang (5.1): der Grundkalender der
        /// Klimaregion, in dem jeder Tag Ruhetag ist, der in mindestens einer der
        /// <paramref name="zonen"/> Ruhetag ist (ihre Ferien, auch die eines gebundenen
        /// Gebäudes). So mittelt die Summe über Tage, an denen alle Zonen nach ihrem Tagtyp
        /// zapfen — ein Ferientag einer Zone drückt den Werktag der Summe nicht. Eine Zone ohne
        /// Kalender (<c>null</c>) zählt nicht.
        /// </summary>
        internal static ZapfTagtyp[] OhneRuhetage(IReadOnlyList<ZapfTagtyp> grund,
                                                   IEnumerable<IReadOnlyList<ZapfTagtyp>> zonen)
        {
            if (grund == null || grund.Count != Zapfkalender.TAGE)
                throw new ArgumentException("Der Kalender trägt nicht 365 Tagtypen.", nameof(grund));
            var kalender = new ZapfTagtyp[Zapfkalender.TAGE];
            for (int d = 0; d < kalender.Length; d++) kalender[d] = grund[d];
            if (zonen == null) return kalender;
            foreach (IReadOnlyList<ZapfTagtyp> z in zonen)
            {
                if (z == null) continue;
                if (z.Count != Zapfkalender.TAGE)
                    throw new ArgumentException("Ein Zonenkalender trägt nicht 365 Tagtypen.", nameof(zonen));
                for (int d = 0; d < kalender.Length; d++)
                    if (z[d] == ZapfTagtyp.Ruhetag) kalender[d] = ZapfTagtyp.Ruhetag;
            }
            return kalender;
        }

        /// <summary>Der Index eines Tagtyps in den drei Tagesgängen; −1 für den Ruhetag.</summary>
        private static int Index(ZapfTagtyp typ)
        {
            switch (typ)
            {
                case ZapfTagtyp.Werktag: return 0;
                case ZapfTagtyp.Samstag: return 1;
                case ZapfTagtyp.SonnFeiertag: return 2;
                default: return -1;
            }
        }

        private static IReadOnlyList<double> Mittel(double[,] summen, int[] zaehler, int t)
        {
            if (zaehler[t] == 0) return null;
            var w = new double[Zapfkalender.STUNDEN_TAG];
            for (int s = 0; s < Zapfkalender.STUNDEN_TAG; s++) w[s] = summen[t, s] / zaehler[t];
            return Array.AsReadOnly(w);
        }
    }
}
