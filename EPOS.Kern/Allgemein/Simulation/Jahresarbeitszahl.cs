namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Jahresarbeitszahl der Wärmepumpe — die EINE Formel des Kerns.</b> Die Kennzahl
    /// <c>eff.jaz</c> des Berichts (<see cref="KennzahlenKatalog"/>), der Block „Strom“ des
    /// Wärmepumpen-Reiters und die Spalte JAZ seiner Modultabelle
    /// (<see cref="SimulationErgebnisCtrl.WaermepumpeErgebnis"/>,
    /// <see cref="SimulationErgebnisCtrl.WpModulZeile"/>) rufen sie; keine dieser Stellen teilt selbst.
    ///
    /// <para><b>Zwei Bilanzgrenzen, eine Rechnung.</b> Die Jahresarbeitszahl des SYSTEMS mit Heizstab
    /// (<see cref="MitHeizstab"/>, Bilanzgrenze nach VDI 4650) zählt die Wärme des Heizstabs in den
    /// Zähler und seinen Strom in den Nenner; die der Wärmepumpe ALLEIN (<see cref="Waermepumpe"/>)
    /// ist derselbe Quotient ohne Heizstab. Der Heizstab rechnet mit der Leistungszahl 1
    /// (<c>SimulationWaermepumpe.Heizstabphase</c>): Seine Wärme IST sein Strom, deshalb trägt die
    /// Formel ihn als EINE Menge.</para>
    ///
    /// <para>Die Mengen kommen in DERSELBEN Einheit herein (die Aufrufer reichen MWh/a); die
    /// Jahresarbeitszahl ist dimensionslos. Ohne Strom — Nenner 0, negativ oder keine Zahl — gibt es
    /// keine Jahresarbeitszahl: <c>null</c>, die Anzeige schreibt einen Strich. Der Kältestrom einer
    /// reversiblen Wärmepumpe gehört nicht hierher; er hat seine eigene Kennzahl
    /// (<see cref="Kaeltekaskade"/>, Jahresarbeitszahl Kälte).</para>
    /// </summary>
    public static class Jahresarbeitszahl
    {
        /// <summary>
        /// Die Jahresarbeitszahl des Systems mit Heizstab [–] =
        /// (Wärme der Wärmepumpe + Heizstab) ÷ (Strom der Wärmepumpe + Heizstab); <c>null</c> ohne Strom.
        /// </summary>
        /// <param name="waermeWp">Wärmeproduktion der Wärmepumpe ohne Heizstab.</param>
        /// <param name="stromWp">Strombedarf der Wärmepumpe ohne Heizstab, samt Mehrstrom aus Takten.</param>
        /// <param name="heizstab">Wärme des Heizstabs, gleich seinem Strom (Leistungszahl 1).</param>
        public static double? MitHeizstab(double waermeWp, double stromWp, double heizstab)
        {
            double nenner = stromWp + heizstab;
            return nenner > 0 ? (double?)((waermeWp + heizstab) / nenner) : null;
        }

        /// <summary>
        /// Die Jahresarbeitszahl der Wärmepumpe allein [–] = Wärme der Wärmepumpe ÷ Strom der
        /// Wärmepumpe; <c>null</c> ohne Strom. Derselbe Quotient wie <see cref="MitHeizstab"/>, mit
        /// dem Heizstab 0 — die Addition von 0 lässt beide Mengen bitgleich.
        /// </summary>
        public static double? Waermepumpe(double waermeWp, double stromWp)
            => MitHeizstab(waermeWp, stromWp, 0.0);
    }
}
