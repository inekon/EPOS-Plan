using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das Zahlenformat der Eingabefelder als Kernregel (Umsetzungskonzept iU4,
    /// Schritt 1).
    ///
    /// <para><b>Warum hier.</b> Die Rümpfe standen bis dahin in <c>Program</c>, also
    /// in der Klasse mit dem WinForms-Einstiegspunkt. Gebraucht werden sie aber auch
    /// von Kern-Code ohne Oberfläche (<c>EmissionenCtrl.WertEingeben</c>). Die
    /// Rümpfe sind wortgleich übernommen; <c>Program.ZahlParsen</c> und
    /// <c>Program.GanzzahlParsen</c> leiten nur noch hierher weiter, damit die
    /// Aufrufer in den Masken unverändert bleiben.</para>
    /// </summary>
    public static class ZahlText
    {
        /// <summary>
        /// Parst eine Zahl mit Dezimal-Komma ODER -Punkt. Gleiche Regel wie
        /// WaermequelleClass.ZahlParsen, nur in double-Genauigkeit - gedacht für
        /// Eingabefelder, deren Wert als double weiterverarbeitet wird.
        /// Kein Tausendertrennzeichen: "1.234,5" wird bewusst abgelehnt, statt
        /// wie double.Parse(CurrentCulture) still zu 12345 zu werden.
        /// </summary>
        public static bool Parsen(string text, out double wert)
        {
            wert = 0.0;
            if (string.IsNullOrEmpty(text)) return false;
            text = text.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out wert);
        }

        /// <summary>
        /// Ganzzahl-Gegenstück zu <see cref="Parsen"/>: invariant geparst.
        /// Komma und Punkt sind hier bewusst KEINE gültigen Zeichen - es geht um
        /// Stückzahlen, Tage, Nutzungsdauern und ganze Grad.
        /// </summary>
        public static bool GanzzahlParsen(string text, out int wert)
        {
            wert = 0;
            if (string.IsNullOrEmpty(text)) return false;
            return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out wert);
        }
    }
}
