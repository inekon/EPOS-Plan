namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die eine Stelle, an der die Investitionssumme eines BHKW definiert ist.
    /// <para>
    /// <b>Nutzerentscheid (22.08.2026): Die Einzelposten fuehren.</b> Modul, Montage und
    /// Inbetriebnahme, Lieferung, Schallschutzhaube und Abgasreinigung
    /// (<c>Kosten_Modul</c>, <c>Kosten_Montage</c>, <c>Kosten_Lieferung</c>,
    /// <c>Kosten_Schallschutzhaube</c>, <c>Kosten_Abgasreinigung</c>) werden frei
    /// erfasst; ihre Summe IST die Investition. Der spezifische Wert
    /// <c>Investition_kwel</c> [EUR/kWel] wird daraus abgeleitet und ist nur noch
    /// Anzeige - im Dialog schreibgeschuetzt, beim Speichern nachgezogen.
    /// </para>
    /// <para>
    /// <b>Warum.</b> Zur selben Investitionssumme fuehrten zwei unabhaengige Wege, und sie
    /// liefen auseinander: Modul <c>A-Tron_21_F</c> traegt Pel = 21,00 kW, alle
    /// Einzelposten 0,00 EUR und <c>Investition_kwel</c> = 2000 - also 0 EUR auf dem einen
    /// und 42.000 EUR auf dem anderen Weg. Beide Wege sind in
    /// <c>TechnikPlanwertCtrl.BasenFuellen</c> als Kostenbasis waehlbar
    /// (<c>BASIS_MODULPREIS</c> bzw. <c>BASIS_SPEZIFISCH</c> = Investition_kwel * Pel),
    /// sie muessen deshalb dieselbe Groesse ergeben.
    /// </para>
    /// <para>
    /// Aufrufer: <c>Form_DBBHKW</c> (Anzeige und Pruefung), <c>BHKWStammCtrl.Update</c> und
    /// <c>BHKWCtrl.Update</c> (Schreibwege). <c>BHKWCtrl.CopyFromStamm</c> rechnet bewusst
    /// NICHT nach: es kopiert einen Stammsatz unveraendert ins Projekt, Bestandsdaten
    /// bleiben dabei so, wie sie sind (der Bestandsabgleich ist ein eigener Schritt).
    /// </para>
    /// </summary>
    public static class BHKWKosten
    {
        /// <summary>Summe der fuenf Einzelposten [EUR] - die Investition des Geraets.</summary>
        public static double Summe(double modul, double montage, double lieferung,
                                   double schallschutzhaube, double abgasreinigung)
        {
            return modul + montage + lieferung + schallschutzhaube + abgasreinigung;
        }

        /// <summary>
        /// true, wenn sich aus der Summe ein spezifischer Wert je kWel bilden laesst.
        /// Bei <paramref name="pel"/> = 0 ist er es nicht: jede Zahl mal 0 ergaebe wieder
        /// 0 und wuerde die erfasste Summe verschweigen. Der Dialog zeigt dann
        /// ausdruecklich "nicht bestimmbar" statt einer erfundenen 0,00.
        /// </summary>
        public static bool JeKWelBestimmbar(double pel)
        {
            return pel > 0.0;
        }

        /// <summary>
        /// Der abgeleitete spezifische Wert [EUR/kWel] = Summe / Pel. Ist er nicht
        /// bestimmbar (Pel = 0), liefert die Methode 0 - dann steht in der Spalte keine
        /// Zahl mehr, die zu den Posten nicht mehr passt. Die Summe selbst bleibt in den
        /// Postenspalten erhalten und geht von dort in die Kostenrechnung.
        /// </summary>
        public static double JeKWel(double summe, double pel)
        {
            return JeKWelBestimmbar(pel) ? summe / pel : 0.0;
        }
    }
}
