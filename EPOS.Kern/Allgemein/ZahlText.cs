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

        /// <summary>
        /// Der Vertrag von <c>Program.convertTxt2Double</c> als KERNREGEL
        /// (iU9-W13.0c): leer oder <c>null</c> ergibt 0, nicht parsbarer Text wirft
        /// <see cref="System.FormatException"/> — die Einlesewege fangen sie und
        /// zaehlen den Eintrag als Fehler.
        ///
        /// <para><b>Warum hier.</b> Die vier Katalogimporte rechnen mit genau diesem
        /// Vertrag, und ihre Rechnung zieht mit Welle 13 in den Kern. <c>Program.*</c>
        /// ist dort verboten (iU5-Waechter); der Rumpf ist wortgleich uebernommen,
        /// damit die Zahl dieselbe bleibt.</para>
        /// </summary>
        public static double NachDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            double zahl;
            if (Parsen(text, out zahl)) return zahl;
            throw new System.FormatException("Keine gültige Zahl: \"" + text + "\"");
        }

        /// <summary>
        /// Der Vertrag von <c>Program.convertTxt2Int</c> als Kernregel: leer oder
        /// nicht (ganzzahlig) parsbar ergibt 0, kein Wurf. Zusaetzlich werden
        /// Dezimalschreibweisen ganzer Zahlen angenommen ("35.0" und "35,0" ergeben
        /// 35) — VDI-Dateien fuehren Ganzzahlfelder teils so.
        /// </summary>
        public static int NachInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            int ganz;
            if (GanzzahlParsen(text, out ganz)) return ganz;

            double zahl;
            if (Parsen(text, out zahl) && zahl >= int.MinValue && zahl <= int.MaxValue
                && zahl == System.Math.Floor(zahl))
                return (int)zahl;

            return 0;
        }
    }
}
