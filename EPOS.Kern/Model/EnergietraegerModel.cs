namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Energieträger, wie ihn <see cref="KostenSummenCtrl.GetAllCarriers"/> aus
    /// <c>energy_carrier</c> (+ <c>pricing_model</c>) liest: Stammdaten, Heizwerte,
    /// Preisbestandteile und Emissionsfaktoren.
    /// </summary>
    /// <remarks>
    /// Stand bis iU9-W0 zusammen mit <see cref="EnergyConversion"/> am Ende von
    /// <c>Views\Kosten\Form_Kosten.cs</c>. Mit der Stilllegung jener Maske
    /// (Anwenderentscheid iF29) sind beide Klassen unverändert hierher gezogen — sie
    /// sind reine Datenhalter ohne Oberflächenbezug und werden von der
    /// Energieträgerverwaltung, dem Trägerkatalog und den Kesselmasken gebraucht.
    /// </remarks>
    public class EnergyCarrier
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string PricingModel { get; set; } // GAS, FUEL, GRID
        public string Code { get; set; }                                      // Das ist der Standard-Heizwert aus der Tabelle ENERGY_CARRIER
        public double HiKwhPerUnit { get; set; }
        public double HsKwhPerUnit { get; set; }
        public string GroupCode { get; set; }
        public string BillingUnit { get; set; }
        public int ID_Brennstoff { get; set; }
        public double price_work { get; set; }
        public double price_base { get; set; }
        public double price_power { get; set; }
        public double CO2 { get; set; }
        public double SO2 { get; set; }
        public double NOx { get; set; }
        public bool HasPowerPrice { get; set; }
        public bool HasHi { get; set; }
        public bool HasHs { get; set; }
    }

    /// <summary>
    /// Ein Umrechnungssatz zwischen zwei Abrechnungseinheiten desselben Brennstoffs
    /// (gelesen von <c>ucFuelSettings.GetConversions</c>).
    /// </summary>
    public class EnergyConversion
    {
        public int IDBrennstoff { get; set; }
        public string FromUnit { get; set; }
        public string ToUnitCode { get; set; } // z.B. "kg", "L"
        public double Factor { get; set; }

        // Hilfseigenschaft für die ComboBox-Anzeige
        public string ToUnitLabel => $"{ToUnitCode} (Faktor: {Factor})";
    }
}
