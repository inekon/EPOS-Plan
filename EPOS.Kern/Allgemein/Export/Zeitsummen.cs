using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Summen einer Jahresreihe je Monat, Woche und Tag</b> — die Säulenbilder des
    /// Grafikreiters „Wärme-/Strombedarf“ und ihr CSV-Export rechnen hier, nicht in der Oberfläche.
    ///
    /// <para><b>Gemeinjahr ohne Jahresdatum</b> (feste Raster des Rechenkerns): 365 Tage, die Monate
    /// mit 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 Tagen. <b>Woche</b> heißt sieben Tage ab dem
    /// 1. Januar: 52 × 7 = 364 Tage, und der 365. Tag gehört zur 52. Woche — sie zählt acht Tage.
    /// Ein Wochentagsraster oder eine Kalenderwoche nach ISO kennt die Reihe nicht.</para>
    ///
    /// <para><b>Die Reihe ist eine Leistung</b> [kW] je Stützstelle: 8 760 Stundenwerte oder 35 040
    /// Viertelstundenwerte. Eine Stützstelle trägt damit die Energie Leistung × Dauer — eine Stunde
    /// bzw. eine Viertelstunde —, die Summen verlassen den Kern in MWh (Einheitenregel 2).</para>
    /// </summary>
    public static class Zeitsummen
    {
        /// <summary>Tage je Monat im Gemeinjahr.</summary>
        public static readonly int[] MONATSTAGE = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Wochen des Gemeinjahrs; die letzte trägt den 365. Tag mit.</summary>
        public const int WOCHEN = 52;

        /// <summary>Tage des Gemeinjahrs.</summary>
        public const int TAGE = 365;

        /// <summary>Die Anzahl der Fächer eines Rasters; <c>0</c> für ein Raster ohne Summenbildung.</summary>
        public static int Faecher(Zeitraster raster) => raster switch
        {
            Zeitraster.Monat => 12,
            Zeitraster.Woche => WOCHEN,
            Zeitraster.Tag => TAGE,
            _ => 0
        };

        /// <summary>
        /// Die nullbasierte Woche eines nullbasierten Tages: <c>tag / 7</c>; der 365. Tag (Index 364)
        /// gehört zur 52. Woche (Index 51).
        /// </summary>
        public static int WocheDesTages(int tag) => Math.Min(tag / 7, WOCHEN - 1);

        /// <summary>Der nullbasierte Monat eines nullbasierten Tages im Gemeinjahr.</summary>
        public static int MonatDesTages(int tag)
        {
            int rest = tag;
            for (int m = 0; m < 12; m++)
            {
                if (rest < MONATSTAGE[m]) return m;
                rest -= MONATSTAGE[m];
            }
            return 11;
        }

        /// <summary>
        /// Die Energiesummen [MWh] einer Leistungsreihe [kW] je Fach des Rasters (12 Monate, 52 Wochen
        /// oder 365 Tage). <c>null</c>, wenn die Reihe weder 8 760 noch 35 040 Werte trägt oder das
        /// Raster keine Summen kennt. Nicht endliche Werte zählen als null.
        /// </summary>
        public static double[] SummenMwh(double[] reihe, Zeitraster raster)
        {
            int faecher = Faecher(raster);
            if (reihe == null || faecher == 0) return null;
            if (reihe.Length != 8760 && reihe.Length != 35040) return null;

            int jeTag = reihe.Length / TAGE;          // 24 oder 96 Stützstellen je Tag
            double dauer = 24.0 / jeTag;              // Stunden je Stützstelle
            var summen = new double[faecher];
            for (int i = 0; i < reihe.Length; i++)
            {
                double w = reihe[i];
                if (!double.IsFinite(w)) continue;
                int tag = i / jeTag;
                int fach = raster switch
                {
                    Zeitraster.Monat => MonatDesTages(tag),
                    Zeitraster.Woche => WocheDesTages(tag),
                    _ => tag
                };
                summen[fach] += w * dauer * 0.001;   // kWh -> MWh wie BhkwPlan.MonatsSumme
            }
            return summen;
        }
    }
}
