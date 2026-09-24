namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Einheiten von gbXML nach SI</b> (Datenaustauschkonzept 3.2; Aufzählungswerte nach
    /// Befund R 1.5 und 5.1). Jede Funktion rechnet einen Zahlenwert aus der genannten Einheit um und
    /// liefert <c>null</c> für eine unbekannte Einheit — nie 0 und nie geraten. Die Meldung
    /// <c>IMP_GBXML_PROT_EINHEIT_UNBEKANNT</c> legt der Leser, der weiß, wo die Zahl stand.
    ///
    /// <para><b>Welche Einheit gilt</b>, entscheidet der Leser: Die vier Pflichtattribute der Wurzel
    /// (<c>lengthUnit</c>, <c>areaUnit</c>, <c>volumeUnit</c>, <c>temperatureUnit</c>) gelten
    /// global, ein lokales <c>unit</c> schlägt das globale. Bei <c>Conductivity</c>,
    /// <c>Density</c>, <c>SpecificHeat</c>, <c>U-value</c>, <c>R-value</c>, <c>PeopleNumber</c>
    /// und den Leistungsdichten ist <c>unit</c> Pflicht; fehlt es, ist die Einheit unbekannt.</para>
    ///
    /// <para><b>Die Faktoren sind Definitionen, keine Messwerte:</b> Fuß 0,3048 m, Zoll 0,0254 m,
    /// Yard 0,9144 m, Meile 1 609,344 m, Pfund 0,45359237 kg, BTU (IT) 1 055,05585262 J,
    /// Grad Fahrenheit bzw. Rankine als Temperaturdifferenz 5/9 K. Jede abgeleitete Einheit
    /// entsteht aus diesen, nicht aus einer gerundeten Tabellenzahl.</para>
    /// </summary>
    internal static class GbxmlEinheiten
    {
        /// <summary>Fuß in Meter (Definition).</summary>
        public const double FUSS_M = 0.3048;
        /// <summary>Zoll in Meter (Definition).</summary>
        public const double ZOLL_M = 0.0254;
        /// <summary>Yard in Meter (Definition).</summary>
        public const double YARD_M = 0.9144;
        /// <summary>Meile in Meter (Definition).</summary>
        public const double MEILE_M = 1609.344;
        /// <summary>Pfund in Kilogramm (Definition).</summary>
        public const double PFUND_KG = 0.45359237;
        /// <summary>British Thermal Unit (IT) in Joule (Definition).</summary>
        public const double BTU_J = 1055.05585262;
        /// <summary>Kelvin je Grad Fahrenheit (Temperaturdifferenz).</summary>
        public const double KELVIN_JE_FAHRENHEIT = 5.0 / 9.0;

        /// <summary>Die Personenangabe als Anzahl (<c>PeopleNumber unit="NumberOfPeople"</c>).</summary>
        public const string PERSONEN_ANZAHL = "NumberOfPeople";

        /// <summary>Länge in Meter (<c>lengthUnitEnum</c>).</summary>
        public static double? Laenge(double wert, string einheit)
        {
            double? f = Laengenfaktor(einheit);
            return f.HasValue ? wert * f.Value : (double?)null;
        }

        /// <summary>Fläche in Quadratmeter (<c>areaUnitEnum</c>).</summary>
        public static double? Flaeche(double wert, string einheit)
        {
            if (einheit == null || !einheit.StartsWith("Square", System.StringComparison.Ordinal)) return null;
            double? f = Laengenfaktor(einheit.Substring("Square".Length));
            return f.HasValue ? wert * f.Value * f.Value : (double?)null;
        }

        /// <summary>Volumen in Kubikmeter (<c>volumeUnitEnum</c>).</summary>
        public static double? Volumen(double wert, string einheit)
        {
            if (einheit == null || !einheit.StartsWith("Cubic", System.StringComparison.Ordinal)) return null;
            double? f = Laengenfaktor(einheit.Substring("Cubic".Length));
            return f.HasValue ? wert * f.Value * f.Value * f.Value : (double?)null;
        }

        /// <summary>Temperatur in Grad Celsius (<c>temperatureUnitEnum</c>: F, C, K, R).</summary>
        public static double? Temperatur(double wert, string einheit)
        {
            switch (einheit)
            {
                case "C": return wert;
                case "F": return (wert - 32.0) * KELVIN_JE_FAHRENHEIT;
                case "K": return wert - 273.15;
                case "R": return (wert - 491.67) * KELVIN_JE_FAHRENHEIT;
                default: return null;
            }
        }

        /// <summary>Wärmeleitfähigkeit in W/(mK) (<c>conductivityUnitEnum</c>).</summary>
        public static double? Leitfaehigkeit(double wert, string einheit)
        {
            switch (einheit)
            {
                case "WPerMeterK": return wert;
                case "WPerCmC": return wert * 100.0;
                case "BtuPerHourFtF": return wert * BTU_J / 3600.0 / FUSS_M / KELVIN_JE_FAHRENHEIT;
                default: return null;
            }
        }

        /// <summary>Rohdichte in kg/m³ (<c>densityUnitEnum</c>).</summary>
        public static double? Dichte(double wert, string einheit)
        {
            switch (einheit)
            {
                case "KgPerCubicM": return wert;
                case "KgPerCubicCm": return wert * 1.0e6;
                case "LbsPerCubicFt": return wert * PFUND_KG / (FUSS_M * FUSS_M * FUSS_M);
                case "LbsPerCubicIn": return wert * PFUND_KG / (ZOLL_M * ZOLL_M * ZOLL_M);
                default: return null;
            }
        }

        /// <summary>Spezifische Wärmekapazität in J/(kgK) (<c>specificHeatEnum</c>).</summary>
        public static double? Waermekapazitaet(double wert, string einheit)
        {
            switch (einheit)
            {
                case "JPerKgK": return wert;
                case "BTUPerLbF": return wert * BTU_J / PFUND_KG / KELVIN_JE_FAHRENHEIT;
                default: return null;
            }
        }

        /// <summary>U-Wert in W/(m²K) (<c>uValueUnitEnum</c>: nur diese zwei, Befund R 1.5).</summary>
        public static double? UWert(double wert, string einheit)
        {
            switch (einheit)
            {
                case "WPerSquareMeterK": return wert;
                case "BtuPerHourSquareFtF": return wert * BTU_J / 3600.0 / (FUSS_M * FUSS_M) / KELVIN_JE_FAHRENHEIT;
                default: return null;
            }
        }

        /// <summary>Wärmedurchlasswiderstand in m²K/W (<c>resistanceUnitEnum</c>).</summary>
        public static double? RWert(double wert, string einheit)
        {
            switch (einheit)
            {
                case "SquareMeterKPerW": return wert;
                case "HrSquareFtFPerBTU": return wert * (FUSS_M * FUSS_M) * KELVIN_JE_FAHRENHEIT / (BTU_J / 3600.0);
                default: return null;
            }
        }

        /// <summary>Leistung je Fläche in W/m² (<c>LightPowerPerArea</c>, <c>EquipPowerPerArea</c>).</summary>
        public static double? LeistungJeFlaeche(double wert, string einheit)
        {
            switch (einheit)
            {
                case "WattPerSquareMeter": return wert;
                case "WattPerSquareFoot": return wert / (FUSS_M * FUSS_M);
                default: return null;
            }
        }

        /// <summary>
        /// Fläche je Person in m² (<c>PeopleNumber</c> mit <c>SquareMPerPerson</c> bzw.
        /// <c>SquareFtPerPerson</c>); <c>null</c> für <see cref="PERSONEN_ANZAHL"/> (das ist eine
        /// Anzahl, keine Fläche) und für jede unbekannte Einheit.
        /// </summary>
        public static double? FlaecheJePerson(double wert, string einheit)
        {
            switch (einheit)
            {
                case "SquareMPerPerson": return wert;
                case "SquareFtPerPerson": return wert * FUSS_M * FUSS_M;
                default: return null;
            }
        }

        /// <summary>Ist die Einheit einer Personenangabe eine der drei bekannten?</summary>
        public static bool PersonenEinheitBekannt(string einheit)
            => einheit == PERSONEN_ANZAHL || einheit == "SquareMPerPerson" || einheit == "SquareFtPerPerson";

        /// <summary>Anteil [–] (<c>SolarHeatGainCoeff</c>, <c>Transmittance</c>: <c>Fraction</c> oder <c>Percent</c>).</summary>
        public static double? Anteil(double wert, string einheit)
        {
            switch (einheit)
            {
                case "Fraction": return wert;
                case "Percent": return wert / 100.0;
                default: return null;
            }
        }

        private static double? Laengenfaktor(string einheit)
        {
            switch (einheit)
            {
                case "Meters": return 1.0;
                case "Millimeters": return 0.001;
                case "Centimeters": return 0.01;
                case "Kilometers": return 1000.0;
                case "Feet": return FUSS_M;
                case "Inches": return ZOLL_M;
                case "Yards": return YARD_M;
                case "Miles": return MEILE_M;
                default: return null;
            }
        }
    }
}
