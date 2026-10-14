using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Bereitschaftsverlust des Heizkessels in seiner Einheit</b> (Anwenderentscheid vom
    /// 02.10.2026): gespeichert werden Wert (<c>Betriebsbereitschaftverlust</c>) und Einheit
    /// (<c>Bereitschaft_Einheit</c>, <see cref="DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW"/> oder
    /// <see cref="DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT"/>), gerechnet wird in kW. Diese
    /// Klasse ist die EINE Stelle, an der aus beiden eine Leistung wird, und die EINE Stelle der
    /// Prüfgrenzen je Einheit — Dialog, Katalogbrowser und Rechenweg fragen sie.
    /// </summary>
    public static class KesselBereitschaft
    {
        /// <summary>Die beiden zulässigen Einheiten, in Anzeigereihenfolge.</summary>
        public static readonly string[] EINHEITEN =
        {
            DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW,
            DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT
        };

        /// <summary>
        /// Die Einheit eines gespeicherten oder eingegebenen Textes: „%" bleibt Prozent, alles
        /// andere — leer, NULL aus einer nicht migrierten Datenbank, „kW" in jeder Schreibweise —
        /// ist kW. Kein Wert wird damit umgedeutet: kW ist die Einheit, in der der Bestand rechnet.
        /// </summary>
        public static string Einheit(string roh)
        {
            string t = (roh ?? "").Trim();
            return t == DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT
                ? DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT
                : DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_KW;
        }

        /// <summary>Steht der Wert in Prozent der Nennleistung?</summary>
        public static bool IstProzent(string einheit)
            => Einheit(einheit) == DbWerte.KESSEL_BEREITSCHAFT_EINHEIT_PROZENT;

        /// <summary>
        /// <b>Die Bereitschaftsleistung [kW]</b>, mit der der Lauf rechnet: der Wert selbst bei kW,
        /// <c>Wert × Nennleistung / 100</c> bei Prozent. Negativ, NaN oder nicht gesetzt heißt:
        /// kein Bereitschaftsverlust; ohne Nennleistung ergibt ein Prozentwert 0.
        /// </summary>
        /// <param name="wert">Der gespeicherte Wert in seiner Einheit.</param>
        /// <param name="einheit">Die gespeicherte Einheit.</param>
        /// <param name="nennleistungKw">Die Nennleistung des Kessels [kW] (<c>Ptherm</c>).</param>
        public static double LeistungKw(double wert, string einheit, double nennleistungKw)
        {
            if (double.IsNaN(wert) || wert <= 0) return 0;
            if (!IstProzent(einheit)) return wert;
            if (double.IsNaN(nennleistungKw) || nennleistungKw <= 0) return 0;
            return wert * nennleistungKw / 100.0;
        }

        /// <summary>
        /// Die Obergrenze des Werts in seiner Einheit: 100 bei Prozent, die Nennleistung bei kW;
        /// <c>null</c> = keine (kW ohne gepflegte Nennleistung).
        /// </summary>
        public static double? Obergrenze(string einheit, double? nennleistungKw)
        {
            if (IstProzent(einheit)) return 100.0;
            return nennleistungKw.HasValue && nennleistungKw.Value > 0 ? nennleistungKw : null;
        }

        /// <summary>
        /// <b>Die Prüfgrenzen je Einheit</b>: kW 0 … Nennleistung (ohne gepflegte Nennleistung
        /// nur „nicht negativ"), Prozent 0 … 100. Rückgabe: der Ablehnungsgrund im Klartext oder
        /// <c>null</c>.
        /// </summary>
        /// <param name="wert">Der eingegebene Wert.</param>
        /// <param name="einheit">Die gewählte Einheit.</param>
        /// <param name="nennleistungKw">Die Nennleistung des Kessels [kW]; <c>null</c> = unbekannt.</param>
        /// <param name="feldname">Die Beschriftung des Felds für den Grund.</param>
        public static string Verstoss(double wert, string einheit, double? nennleistungKw, string feldname)
        {
            double? max = Obergrenze(einheit, nennleistungKw);
            bool zahl = !double.IsNaN(wert) && !double.IsInfinity(wert);
            if (zahl && wert >= 0 && (!max.HasValue || wert <= max.Value)) return null;

            if (!max.HasValue)
                return string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KBROW_MSG_WERT_NEGATIV, feldname);
            return string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KBROW_MSG_WERT_BEREICH, feldname,
                0.0.ToString(CultureInfo.CurrentCulture),
                max.Value.ToString(CultureInfo.CurrentCulture));
        }

        /// <summary>Wert mit Einheit für eine Anzeige, etwa „0,075 kW" oder „0,5 %".</summary>
        public static string Anzeige(double wert, string einheit, CultureInfo kultur = null)
            => wert.ToString("0.###", kultur ?? CultureInfo.CurrentCulture) + " " + Einheit(einheit);
    }
}
