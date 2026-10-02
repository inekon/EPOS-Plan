using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Regeln des Solarkreises</b> — Verluste, Pumpenstrom, Bezugsfläche,
    /// Einfallswinkelkorrektur und Arbeitstemperatur des Kollektorfelds (Welle M2 der
    /// Entscheidungsvorlage „Modellgrenzen der Rechenwege": ST1 bis ST6). Ohne Datenbank und
    /// ohne Zustand: <see cref="SimulationSolarthermie"/> ruft sie im Lauf, die Tests rufen sie
    /// gegen die Handrechnung.
    ///
    /// <para><b>Jede Vorgabe rechnet wie vor der Welle:</b> Verluste 8 %, Aperturfläche, kein
    /// Pumpenstrom ohne gepflegten Wert, feste Arbeitstemperatur 50 °C, Diffusanteil mit dem
    /// Faktor der Direktstrahlung, solange der Katalog kein <c>K_dfu</c> führt.</para>
    /// </summary>
    public static class Solarkreis
    {
        /// <summary>Verluste des Solarkreises ohne gepflegten Wert [% des Bruttoertrags].</summary>
        public const double VERLUSTE_VORGABE_PROZENT = 8;

        /// <summary>
        /// Der Faktor auf das Bruttopotenzial, der nach den Verlusten des Solarkreises bleibt
        /// (ST3 Stufe 1): <c>(100 − Verluste) / 100</c>. Leer, nicht endlich oder außerhalb
        /// 0 … 50 % gilt die Vorgabe 8 % — der Faktor ist dann bitgleich das Literal 0,92 des
        /// Rechenwegs vor der Welle.
        /// </summary>
        public static double Verlustfaktor(double? verlusteProzent)
        {
            double p = VERLUSTE_VORGABE_PROZENT;
            if (verlusteProzent.HasValue && !double.IsNaN(verlusteProzent.Value) &&
                verlusteProzent.Value >= 0 && verlusteProzent.Value <= SolarthermieFelderSchema.VERLUSTE_MAX_PROZENT)
                p = verlusteProzent.Value;
            return (100.0 - p) / 100.0;
        }

        /// <summary>
        /// Pumpenstrom des Kollektorfelds in EINER Stunde [kWh] (ST1, VDI 6002 Blatt 1,
        /// EN 15316-4-3): <c>E_P = P_P · 1 h</c>, wenn das Feld in der Stunde Wärme abgibt
        /// (Direktdeckung oder Ladung &gt; 0), sonst 0. Ohne gepflegte Pumpenleistung hilfsweise
        /// der Hilfsenergieanteil der Anlage [%] auf die in der Stunde genutzte Wärme; ohne
        /// beides 0.
        /// </summary>
        /// <param name="pumpenleistungW">Pumpenleistung [W]; leer oder ≤ 0 = nicht gepflegt.</param>
        /// <param name="hilfsenergieAnteilProzent"><c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> [%].</param>
        /// <param name="genutztKwh">In der Stunde genutzte Wärme des Felds (Deckung plus Ladung) [kWh].</param>
        public static double PumpenstromKwh(double? pumpenleistungW, double? hilfsenergieAnteilProzent,
                                            double genutztKwh)
        {
            if (!(genutztKwh > 0)) return 0;
            if (pumpenleistungW.HasValue && pumpenleistungW.Value > 0)
                return pumpenleistungW.Value / 1000.0;
            if (hilfsenergieAnteilProzent.HasValue && hilfsenergieAnteilProzent.Value > 0)
                return genutztKwh * hilfsenergieAnteilProzent.Value / 100.0;
            return 0;
        }

        /// <summary>Rechnet das Feld überhaupt einen Pumpenstrom?</summary>
        public static bool RechnetPumpenstrom(double? pumpenleistungW, double? hilfsenergieAnteilProzent)
            => (pumpenleistungW.HasValue && pumpenleistungW.Value > 0) ||
               (hilfsenergieAnteilProzent.HasValue && hilfsenergieAnteilProzent.Value > 0);
    }
}
