using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// AUFTRAG #351 (U23 und U26) — <b>Schreibweise und Herkunft eines
    /// KWK-Zuschlagssatzes an EINER Stelle</b>: das Zahlenformat, der Vergleich mit dem
    /// Vorschlag des <see cref="KwkgSatzRechner"/> und der Vermerk dazu.
    ///
    /// <para><b>Warum das hier steht und nicht je Ausgabeweg.</b> Die beiden Satzfelder
    /// des BHKW-Dialogs tragen seit U26 vier Nachkommastellen, weil die Leistungsstaffel
    /// des § 7 sie erzeugt (300 kW: 5,5667 ct/kWh). Bericht, Herleitungszeile und
    /// Vorschau rundeten dieselbe Größe auf zwei — derselbe Satz sah an zwei Orten
    /// verschieden aus, und wer die Anzeige für den Wert hielt, verlor 0,0033 ct/kWh.
    /// Das Format steht deshalb einmal im Kern; Word, Excel, Erlösrubrik, Vorschau und
    /// Dialog lesen es von hier.</para>
    ///
    /// <para><b>Der Vergleich läuft auf der Stellenzahl des Feldes</b>, nicht auf einer
    /// gerundeten Anzeige: Zwei Sätze, die sich erst in der fünften Stelle
    /// unterscheiden, sind für das Feld derselbe Wert — ein Vermerk „eigener Wert"
    /// wäre dort eine Behauptung über einen Unterschied, den niemand eintippen kann.</para>
    /// </summary>
    public static class KwkgSatzHerkunft
    {
        /// <summary>Nachkommastellen des Satzes — dieselbe Zahl, die das Zahlenfeld des
        /// Dialogs führt (<c>Nachkommastellen="4"</c>).</summary>
        public const int NACHKOMMASTELLEN = 4;

        /// <summary>.NET-Zahlformat für Word, Reiter, Dialog und Vorschau.</summary>
        public const string ZAHLFORMAT = "N4";

        /// <summary>Zellformat des Excel-Blattes — dieselbe Stellenzahl.</summary>
        public const string EXCELFORMAT = "#,##0.0000";

        /// <summary>Einheitenzeichen des Satzes. Es ist kein Anzeigetext, sondern ein
        /// Symbol, und steht deshalb im Quelltext (Drei-Schichten-Regel).</summary>
        public const string EINHEIT = "ct/kWh";

        /// <summary>Der Satz in der Hausschreibweise [ct/kWh], ohne Einheit.</summary>
        public static string Satz(double satzCt, CultureInfo kultur)
        {
            return satzCt.ToString(ZAHLFORMAT, kultur ?? CultureInfo.CurrentCulture);
        }

        /// <summary>Der Satz mit seiner Einheit („5,5667 ct/kWh").</summary>
        public static string SatzMitEinheit(double satzCt, CultureInfo kultur)
        {
            return Satz(satzCt, kultur) + " " + EINHEIT;
        }

        /// <summary>
        /// true, wenn der ANGESETZTE Satz vom Vorschlag abweicht — gemessen auf
        /// <see cref="NACHKOMMASTELLEN"/> Stellen, also auf der Genauigkeit, die das
        /// Feld überhaupt führen kann.
        /// </summary>
        public static bool Abweichend(double angesetztCt, double vorschlagCt)
        {
            return Math.Round(angesetztCt, NACHKOMMASTELLEN, MidpointRounding.AwayFromZero)
                != Math.Round(vorschlagCt, NACHKOMMASTELLEN, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Der Vermerk zur Herkunft des angesetzten Satzes: „Vorschlag", wenn beide
        /// gleich sind, sonst „eigener Wert {0} — Vorschlag {1}". <c>null</c> als
        /// Vorschlag heißt „kein Vorschlag bekannt" (ein gebuchter Stand aus der Zeit
        /// vor diesem Auftrag) — dann bleibt der Vermerk leer, statt einen Vorschlag
        /// von 0,0000 zu behaupten.
        /// </summary>
        public static string Vermerk(double angesetztCt, double? vorschlagCt, CultureInfo kultur)
        {
            if (!vorschlagCt.HasValue) return "";
            if (!Abweichend(angesetztCt, vorschlagCt.Value))
                return MyResource.Resource.WIRT_ERL_HERKUNFT_VORSCHLAG;
            return string.Format(MyResource.Resource.WIRT_ERL_HERKUNFT_EIGEN,
                                 SatzMitEinheit(angesetztCt, kultur),
                                 SatzMitEinheit(vorschlagCt.Value, kultur));
        }

        /// <summary>
        /// Satz und Herkunft in einer Zeile — die Form der Erlösrubrik und der
        /// BHKW-Vorschau: „5,5667 ct/kWh · Vorschlag" bzw.
        /// „eigener Wert 6,0000 ct/kWh — Vorschlag 5,5667 ct/kWh".
        /// </summary>
        public static string SatzUndHerkunft(double angesetztCt, double? vorschlagCt,
                                             CultureInfo kultur)
        {
            string vermerk = Vermerk(angesetztCt, vorschlagCt, kultur);
            if (vermerk.Length == 0) return SatzMitEinheit(angesetztCt, kultur);
            if (!Abweichend(angesetztCt, vorschlagCt.Value))
                return SatzMitEinheit(angesetztCt, kultur) + " · " + vermerk;
            return vermerk;
        }
    }
}
