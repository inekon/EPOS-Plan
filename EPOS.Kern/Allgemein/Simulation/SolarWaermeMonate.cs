using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WÄRME-AUTARKIE der Solarthermie je Monat — das Gegenstück zum Monatsstapel
    /// „Energie-Bedarf &amp; Deckung" der Photovoltaik im Reiter „Autarkie Analyse".
    ///
    /// <para><b>Eine reine Aggregation, kein Rechenweg.</b> Alle drei Eingänge sind
    /// Ergebnisreihen, die der Lauf ohnehin führt:</para>
    /// <list type="bullet">
    ///   <item>der Wärmebedarf des Projekts je Wärmekanal
    ///   (<see cref="SimulationControl.BedarfKanalStuendlich"/>, Kanäle
    ///   <see cref="Kanal.KANAELE_WAERME"/> — die Kälte gehört nicht dazu),</item>
    ///   <item>die DIREKTDECKUNG der Solarthermie je Kanal und Stunde
    ///   (<see cref="SimulationSolarthermie.Direktdeckung_KanalStuendlich"/>),</item>
    ///   <item>ihr Anteil an der bedarfsdeckenden SPEICHERENTLADUNG je Kanal und Stunde
    ///   (<see cref="SimulationSolarthermie.Speicherentladung_KanalStuendlich"/>).</item>
    /// </list>
    ///
    /// <para>Direktdeckung plus Speicheranteil ist genau die Größe, die die Übersicht als
    /// Wärmedeckung der Solarthermie führt (<c>UebersichtKennzahlen.WaermeSolarMwh</c>);
    /// die Jahressumme dieser Klasse ist deshalb dieselbe Zahl. Die DECKUNGSLÜCKE ist der
    /// Rest des Wärmebedarfs — was Kessel, Wärmepumpe, BHKW oder niemand gedeckt hat.</para>
    ///
    /// <para><b>Kalendermonate</b> (31, 28, 31 … Tage, kein Schaltjahr) — wie
    /// <see cref="ChartRenderer.MonatsSummenMWh"/>.</para>
    /// </summary>
    public sealed class SolarWaermeMonate
    {
        private static readonly int[] TAGE_JE_MONAT = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Wärmebedarf je Monat [kWh].</summary>
        public double[] BedarfKwh { get; } = new double[12];

        /// <summary>Direkt von der Solarthermie gedeckter Bedarf je Monat [kWh].</summary>
        public double[] DirektKwh { get; } = new double[12];

        /// <summary>Über den Speicher gedeckter Solaranteil je Monat [kWh].</summary>
        public double[] SpeicherKwh { get; } = new double[12];

        /// <summary>Nicht solar gedeckter Rest je Monat [kWh], nie negativ.</summary>
        public double[] LueckeKwh { get; } = new double[12];

        public double BedarfJahrKwh { get; private set; }
        public double DirektJahrKwh { get; private set; }
        public double SpeicherJahrKwh { get; private set; }
        public double LueckeJahrKwh { get; private set; }

        /// <summary>Solar gedeckt im Jahr [kWh] (direkt + Speicher).</summary>
        public double SolarJahrKwh => DirektJahrKwh + SpeicherJahrKwh;

        /// <summary>Führt der Lauf einen solaren Speicheranteil? Ohne ihn keine Speicherreihe.</summary>
        public bool HatSpeicheranteil => SpeicherJahrKwh > 0.0;

        /// <summary>
        /// Solarer Deckungsanteil des Jahres [%]; <c>null</c> ohne Wärmebedarf.
        /// </summary>
        public double? DeckungsanteilProzent =>
            BedarfJahrKwh > 0.0 ? SolarJahrKwh / BedarfJahrKwh * 100.0 : (double?)null;

        /// <summary>Solarer Deckungsanteil je Monat [%]; 0 in einem Monat ohne Bedarf.</summary>
        public double[] DeckungMonatProzent
        {
            get
            {
                var r = new double[12];
                for (int m = 0; m < 12; m++)
                    r[m] = BedarfKwh[m] > 0.0 ? (DirektKwh[m] + SpeicherKwh[m]) / BedarfKwh[m] * 100.0 : 0.0;
                return r;
            }
        }

        /// <summary>
        /// Aggregiert drei Stundenreihen [kWh] zu Monatssummen. Eine fehlende oder kurze
        /// Reihe zählt, soweit sie reicht; <c>null</c> zählt als 0.
        /// </summary>
        public static SolarWaermeMonate Aggregieren(double[] bedarf, double[] direkt, double[] speicher)
        {
            var e = new SolarWaermeMonate();
            int h0 = 0;
            for (int m = 0; m < 12; m++)
            {
                int hn = h0 + TAGE_JE_MONAT[m] * 24;
                for (int h = h0; h < hn; h++)
                {
                    e.BedarfKwh[m] += Wert(bedarf, h);
                    e.DirektKwh[m] += Wert(direkt, h);
                    e.SpeicherKwh[m] += Wert(speicher, h);
                }
                e.LueckeKwh[m] = Math.Max(0.0, e.BedarfKwh[m] - e.DirektKwh[m] - e.SpeicherKwh[m]);
                h0 = hn;
            }

            for (int m = 0; m < 12; m++)
            {
                e.BedarfJahrKwh += e.BedarfKwh[m];
                e.DirektJahrKwh += e.DirektKwh[m];
                e.SpeicherJahrKwh += e.SpeicherKwh[m];
                e.LueckeJahrKwh += e.LueckeKwh[m];
            }
            return e;
        }

        /// <summary>
        /// Die Monatswerte eines Laufs. <c>null</c>, wenn der Lauf keine Solarthermie
        /// führt.
        /// </summary>
        public static SolarWaermeMonate AusLauf(SimulationControl sim, SimulationWaermebedarf bedarf)
        {
            if (sim == null || sim.simulation_solarthermie == null) return null;

            SimulationSolarthermie st = sim.simulation_solarthermie;
            var b = new double[Kanalsatz.STUNDEN_JAHR];
            var d = new double[Kanalsatz.STUNDEN_JAHR];
            var s = new double[Kanalsatz.STUNDEN_JAHR];

            foreach (int k in Kanal.KANAELE_WAERME)
            {
                Addiere(b, SimulationControl.BedarfKanalStuendlich(bedarf, k));
                Addiere(d, st.Direktdeckung_KanalStuendlich.Zeile(k));
                Addiere(s, st.Speicherentladung_KanalStuendlich.Zeile(k));
            }

            return Aggregieren(b, d, s);
        }

        private static void Addiere(double[] ziel, double[] quelle)
        {
            if (quelle == null) return;
            int n = Math.Min(ziel.Length, quelle.Length);
            for (int i = 0; i < n; i++) ziel[i] += quelle[i];
        }

        private static double Wert(double[] reihe, int h)
            => reihe != null && h < reihe.Length ? reihe[h] : 0.0;
    }
}
