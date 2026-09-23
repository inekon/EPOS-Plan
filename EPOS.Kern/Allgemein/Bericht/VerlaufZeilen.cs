using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E6 — die ZEILEN unter dem Verlauf mit drei Szenarien: die Nulldurchgänge (die
    /// dynamische Amortisation je Stand und Szenario) und die Restwert-Barwerte am
    /// Horizontende, die nicht in den Linien stehen.
    ///
    /// <para><b>Eine Stelle für Seite und Wortbericht</b>: Der Abschnitt der
    /// Wirtschaftlichkeitsseite und das Dreierbild des Wortberichts tragen denselben Satz —
    /// sonst stünde neben demselben Bild zweimal eine andere Formulierung.</para>
    /// </summary>
    public static class VerlaufZeilen
    {
        /// <summary>
        /// „Nulldurchgänge (dynamische Amortisation): Variante 1: Ungünstig 3,02 a · Erwartet
        /// 2,64 a · Günstig 2,28 a; …" — ohne Durchgang im Horizont „keiner im Zeitraum". Leer,
        /// wenn kein gewählter Stand eine Linie hat.
        /// </summary>
        /// <param name="staende">Die gezeichneten Stände; <c>null</c> = alle.</param>
        /// <param name="szenarien">Die gezeichneten Szenarien (Persistenzwerte); <c>null</c> = alle drei.</param>
        public static string Nulldurchgaenge(WirtschaftlichkeitVerlaufSzenarien verlauf,
                                             ICollection<int> staende, ICollection<string> szenarien,
                                             CultureInfo kultur)
        {
            if (verlauf == null) return "";
            kultur = kultur ?? CultureInfo.CurrentCulture;
            string keiner = MyResource.Resource.WIRT_VERL_KEIN_NULL;
            var teile = new List<string>();
            foreach (KeyValuePair<int, string> v in verlauf.Versionen())
            {
                if (staende != null && !staende.Contains(v.Key)) continue;
                var je = new List<string>();
                foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                {
                    if (szenarien != null && !szenarien.Contains(s)) continue;
                    if (verlauf.Differenz(v.Key, s) == null) continue;
                    double? jahr = verlauf.Nulldurchgang(v.Key, s);
                    je.Add(Szenarioname(s) + " " +
                           (jahr.HasValue ? jahr.Value.ToString("N2", kultur) + " a" : keiner));
                }
                if (je.Count > 0) teile.Add(v.Value + ": " + string.Join(" · ", je));
            }
            return teile.Count == 0 ? ""
                : string.Format(kultur, MyResource.Resource.WIRT_VERL_NULLZEILE, string.Join("; ", teile));
        }

        /// <summary>
        /// „Restwert-Barwerte am Horizontende (nicht in den Linien enthalten): Variante 1:
        /// Ungünstig 71.642 · Erwartet 98.032 · Günstig 119.464 €" — als Differenz zur Referenz;
        /// mit ihnen ergibt sich die Kapitalwertdifferenz der Bandbreite. Ein Betrag unter
        /// 0,50 € entfällt; leer, wenn keiner bleibt.
        /// </summary>
        public static string Restwerte(WirtschaftlichkeitVerlaufSzenarien verlauf,
                                       ICollection<int> staende, ICollection<string> szenarien,
                                       CultureInfo kultur)
        {
            if (verlauf == null) return "";
            kultur = kultur ?? CultureInfo.CurrentCulture;
            var teile = new List<string>();
            foreach (KeyValuePair<int, string> v in verlauf.Versionen())
            {
                if (staende != null && !staende.Contains(v.Key)) continue;
                var je = new List<string>();
                foreach (string s in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                {
                    if (szenarien != null && !szenarien.Contains(s)) continue;
                    double? rest = verlauf.RestwertDifferenz(v.Key, s);
                    if (!rest.HasValue || Math.Abs(rest.Value) <= 0.5) continue;
                    je.Add(Szenarioname(s) + " " + rest.Value.ToString("N0", kultur));
                }
                if (je.Count > 0) teile.Add(v.Value + ": " + string.Join(" · ", je) + " €");
            }
            return teile.Count == 0 ? ""
                : string.Format(kultur, MyResource.Resource.WVERL_RESTWERTE, string.Join("; ", teile));
        }

        /// <summary>Der Anzeigename eines Szenarios in der Oberflächensprache (<c>WIRT_SZEN_*</c>).</summary>
        public static string Szenarioname(string szenario)
        {
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.WORST, StringComparison.Ordinal))
                return MyResource.Resource.WIRT_SZEN_WORST;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.BEST, StringComparison.Ordinal))
                return MyResource.Resource.WIRT_SZEN_BEST;
            if (string.Equals(szenario, WirtschaftlichkeitSzenario.ERWARTET, StringComparison.Ordinal))
                return MyResource.Resource.WIRT_SZEN_ERWARTET;
            return szenario ?? "";
        }
    }
}
