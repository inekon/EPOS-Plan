using System;

namespace SpeicherEngine
{
    /// <summary>
    /// Der Eigenverbrauch des Speichersystems (Welle M5, SP1): Standby-Verbrauch und
    /// Selbstentladung - ohne Datenbank, fuer jede Strategie dieselbe Regel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Standby.</b> Batteriemanagement und Wechselrichter im Leerlauf verbrauchen je Intervall
    /// <c>E = P_standby · dt</c>. Gedeckt wird er aus dem PV-Ueberschuss, der nach der Ladung des
    /// Speichers bleibt, der Rest aus dem Netz - nie aus der Batterie, wie es reale Systeme meist
    /// tun. Der Fahrplan der Batterie bleibt damit unberuehrt; der Standby ist eine zusaetzliche
    /// Last hinter dem Speicher.
    /// </para>
    /// <para>
    /// <b>Selbstentladung.</b> Je Intervall geht der Anteil <c>s/100 · dt / 730 h</c> des Inhalts
    /// verloren, hoechstens bis zur unteren SoC-Grenze - unterhalb haelt das Batteriemanagement den
    /// Speicher. Der Verlust steht in <see cref="SpeicherKennzahlen.SelbstentladungKwh"/> und ist ein
    /// Teil von <see cref="SpeicherKennzahlen.SpeicherverlusteKwh"/>.
    /// </para>
    /// </remarks>
    public static class Speichersystem
    {
        /// <summary>
        /// Der Selbstentladeverlust eines Intervalls [kWh]: <c>stand · anteil</c>, hoechstens
        /// <c>stand − untergrenze</c>, nie negativ. Mit <paramref name="anteil"/> = 0 genau 0.
        /// </summary>
        /// <param name="standKwh">Inhalt zu Beginn des Intervalls [kWh].</param>
        /// <param name="untergrenzeKwh">Untere SoC-Grenze [kWh].</param>
        /// <param name="anteil">Anteil je Intervall (<see cref="SpeicherParameter.SelbstentladungJeIntervall"/>).</param>
        public static double Selbstentladung(double standKwh, double untergrenzeKwh, double anteil)
        {
            if (!(anteil > 0.0)) return 0.0;
            double verlust = standKwh * anteil;
            double frei = standKwh - untergrenzeKwh;
            if (verlust > frei) verlust = frei;
            return verlust > 0.0 ? verlust : 0.0;
        }

        /// <summary>
        /// Die Deckung des Standby-Verbrauchs je Intervall: aus dem PV-Ueberschuss nach der Ladung,
        /// sonst aus dem Netz.
        /// </summary>
        /// <param name="lastKw">Last je Intervall [kW].</param>
        /// <param name="pvKw">PV-Erzeugung je Intervall [kW].</param>
        /// <param name="ladungKwh">Ladung des Speichers je Intervall [kWh]; <c>null</c> = keine.</param>
        /// <param name="standbyKw">Standby-Verbrauch [kW]; 0 liefert eine leere Bilanz.</param>
        /// <param name="dtH">Intervalldauer [h].</param>
        /// <exception cref="ArgumentNullException">Wenn Last oder PV fehlt.</exception>
        /// <exception cref="ArgumentException">Bei ungleicher Laenge oder negativem Standby.</exception>
        public static StandbyBilanz Standby(double[] lastKw, double[] pvKw, double[]? ladungKwh,
                                            double standbyKw, double dtH)
        {
            if (lastKw == null) throw new ArgumentNullException(nameof(lastKw));
            if (pvKw == null) throw new ArgumentNullException(nameof(pvKw));
            if (pvKw.Length != lastKw.Length)
                throw new ArgumentException("Last und PV muessen dieselbe Laenge haben.", nameof(pvKw));
            if (ladungKwh != null && ladungKwh.Length != lastKw.Length)
                throw new ArgumentException("Die Ladereihe muss die Laenge der Last haben.", nameof(ladungKwh));
            if (!(standbyKw >= 0.0) || double.IsInfinity(standbyKw))
                throw new ArgumentException("Der Standby-Verbrauch darf nicht negativ sein.", nameof(standbyKw));
            if (!(dtH > 0.0)) throw new ArgumentException("dt muss groesser 0 sein.", nameof(dtH));

            int n = lastKw.Length;
            var ausPv = new double[n];
            var ausNetz = new double[n];
            double summePv = 0.0, summeNetz = 0.0;

            if (standbyKw > 0.0)
            {
                double bedarfKwh = standbyKw * dtH;
                for (int k = 0; k < n; k++)
                {
                    double ueberschussKwh = (pvKw[k] - lastKw[k]) * dtH;
                    if (ladungKwh != null) ueberschussKwh -= ladungKwh[k];
                    if (ueberschussKwh < 0.0) ueberschussKwh = 0.0;

                    double pv = ueberschussKwh < bedarfKwh ? ueberschussKwh : bedarfKwh;
                    double netz = bedarfKwh - pv;
                    ausPv[k] = pv / dtH;
                    ausNetz[k] = netz / dtH;
                    summePv += pv;
                    summeNetz += netz;
                }
            }

            return new StandbyBilanz(ausPv, ausNetz, summePv, summeNetz);
        }
    }

    /// <summary>
    /// Die Deckung des Standby-Verbrauchs: je Intervall aus PV und aus dem Netz [kW] und die
    /// Jahressummen [kWh].
    /// </summary>
    public sealed class StandbyBilanz
    {
        /// <summary>Legt die Bilanz an.</summary>
        public StandbyBilanz(double[] ausPvKw, double[] ausNetzKw, double ausPvKwh, double ausNetzKwh)
        {
            AusPvKw = ausPvKw ?? throw new ArgumentNullException(nameof(ausPvKw));
            AusNetzKw = ausNetzKw ?? throw new ArgumentNullException(nameof(ausNetzKw));
            AusPvKwh = ausPvKwh;
            AusNetzKwh = ausNetzKwh;
        }

        /// <summary>Aus dem PV-Ueberschuss gedeckter Standby je Intervall [kW].</summary>
        public double[] AusPvKw { get; }

        /// <summary>Aus dem Netz gedeckter Standby je Intervall [kW].</summary>
        public double[] AusNetzKw { get; }

        /// <summary>Jahressumme aus PV [kWh].</summary>
        public double AusPvKwh { get; }

        /// <summary>Jahressumme aus dem Netz [kWh].</summary>
        public double AusNetzKwh { get; }

        /// <summary>Der Eigenverbrauch des Speichersystems [kWh/a].</summary>
        public double GesamtKwh => AusPvKwh + AusNetzKwh;
    }
}
