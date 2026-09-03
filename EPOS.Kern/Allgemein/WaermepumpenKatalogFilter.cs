using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die FILTERLOGIK des Waermepumpen-Katalogs (iU9-W7.0b) — woertlich aus
    /// <c>Form_WpFilterAuswahl.ApplyFilter</c> (Z. 66-130) und <c>FillCombo</c> (Z. 211).
    ///
    /// <para><b>Der Vorlaeufer filtert IM SPEICHER, nicht per SQL.</b> <c>LoadData</c>
    /// liest den ganzen Katalog einmal und legt ihn in <c>_allData</c> ab; jeder
    /// Klapplistenwechsel und jeder Tastendruck im Suchfeld laesst danach ein
    /// LINQ-<c>Where</c> ueber diese Liste laufen. Das bleibt so — bei einer Handvoll
    /// hundert Stammsaetzen ist es schneller als neun Abfragen, und es haelt die
    /// Datenbank aus dem Dialog heraus.</para>
    ///
    /// <para><b>Warum die Logik trotzdem im Kern liegt.</b> Regel F4 des Wellenplans:
    /// Die Komponente filtert nicht selbst. Hier ist der Filter reine Rechnung ohne
    /// Datenbank — er ist damit pruefbar (xunit im Kern) und auf iOS unveraendert
    /// verwendbar. Die Razor-Komponente ruft ihn nur.</para>
    ///
    /// <para><b>„Alle" ist ein NULL-Wert, kein Text.</b> Der Vorlaeufer verglich den
    /// Klapplistentext gegen das deutsche Literal „Alle" (<c>cbHersteller.Text == "Alle"</c>).
    /// Das ging, solange die Maske einsprachig war. In der Blazor-Fassung ist „Alle" ein
    /// Anzeigetext aus dem Ressourcenkatalog; der STEUERWERT ist <c>null</c> —
    /// dieselbe Trennung wie bei <c>Sprungziel</c> und <c>DbWerte</c>. Am Verhalten
    /// aendert das nichts (Abweichung A-2 des Protokolls W7).</para>
    /// </summary>
    public static class WaermepumpenKatalogFilter
    {
        /// <summary>
        /// Was der Anwender eingestellt hat. <c>null</c> heisst bei den sieben
        /// Klapplisten „Alle"; die vier Zahlenfelder sind immer belegt (der Vorlaeufer
        /// arbeitet dort mit <c>NumericUpDown.Value</c>, das nie leer ist).
        /// </summary>
        /// <param name="Hersteller">Gleichheitsfilter auf <c>Hersteller</c>.</param>
        /// <param name="Auslegung">Gleichheitsfilter auf <c>Auslegung</c>.</param>
        /// <param name="Funktionsprinzip">Gleichheitsfilter auf <c>Funktionsprinzip</c>.</param>
        /// <param name="Regelung">Gleichheitsfilter auf <c>Regelung</c>.</param>
        /// <param name="Bauart">Gleichheitsfilter auf <c>Bauart</c>.</param>
        /// <param name="Aufstellung">Gleichheitsfilter auf <c>Aufstellung</c>.</param>
        /// <param name="Zuheizung">
        /// Gleichheitsfilter auf <c>ElZuheizung</c> — als ZEICHENKETTE, weil der
        /// Vorlaeufer die Klappliste aus <c>ElZuheizung.ToString()</c> fuellte und
        /// genauso zurueckverglich.
        /// </param>
        /// <param name="VorlaufMin">Untere Grenze fuer <c>MaxVorlauf</c> [°C].</param>
        /// <param name="VorlaufMax">Obere Grenze fuer <c>MaxVorlauf</c> [°C].</param>
        /// <param name="LeistungMin">Untere Grenze fuer <c>MaxLeistung</c> [kW].</param>
        /// <param name="LeistungMax">Obere Grenze fuer <c>MaxLeistung</c> [kW].</param>
        /// <param name="Suche">Suchmuster auf <c>Bezeichnung</c> (siehe <see cref="Anwenden"/>).</param>
        public sealed record Kriterien(
            string Hersteller = null,
            string Auslegung = null,
            string Funktionsprinzip = null,
            string Regelung = null,
            string Bauart = null,
            string Aufstellung = null,
            string Zuheizung = null,
            double VorlaufMin = 0,
            double VorlaufMax = 0,
            double LeistungMin = 0,
            double LeistungMax = 0,
            string Suche = null);

        /// <summary>
        /// Wendet die Kriterien an. Reihenfolge und Vergleiche sind zeichengleich zu
        /// <c>ApplyFilter</c>.
        ///
        /// <para><b>Die Suche kennt Platzhalter.</b> <c>*</c> steht fuer beliebig viele,
        /// <c>?</c> fuer genau ein Zeichen; das Muster wird verankert (<c>^…$</c>).
        /// OHNE Platzhalter ist es eine Teilsuche wie <c>Contains</c> — der Vorlaeufer
        /// liess dafuer die Anker weg. Gross- und Kleinschreibung spielt keine Rolle.
        /// Ein Muster, an dem der regulaere Ausdruck zerbricht, gilt als KEIN Filter:
        /// Der Anwender tippt, und eine halb geschriebene Klammer darf die Liste nicht
        /// leeren.</para>
        /// </summary>
        public static IReadOnlyList<WaermepumpenKatalogZeile> Anwenden(
            IReadOnlyList<WaermepumpenKatalogZeile> zeilen, Kriterien k)
        {
            if (zeilen == null) return Array.Empty<WaermepumpenKatalogZeile>();
            if (k == null) return zeilen;

            string suche = (k.Suche ?? "").Trim();
            bool ohneSuche = string.IsNullOrEmpty(suche) || suche == "*";
            Regex muster = null;

            if (!ohneSuche)
            {
                try
                {
                    // Regex.Escape maskiert Sonderzeichen; die maskierten Platzhalter
                    // werden danach wieder zu ihrer Regex-Bedeutung gemacht.
                    string ausdruck = "^" + Regex.Escape(suche)
                        .Replace("\\*", ".*").Replace("\\?", ".") + "$";

                    // Ohne Platzhalter will der Anwender eine Teilsuche - dann keine Anker.
                    if (!suche.Contains("*") && !suche.Contains("?"))
                        ausdruck = Regex.Escape(suche);

                    muster = new Regex(ausdruck, RegexOptions.IgnoreCase);
                }
                catch
                {
                    ohneSuche = true;
                }
            }

            return zeilen.Where(x =>
                (k.Hersteller == null || x.Hersteller == k.Hersteller) &&
                (k.Auslegung == null || x.Auslegung == k.Auslegung) &&
                (k.Funktionsprinzip == null || x.Funktionsprinzip == k.Funktionsprinzip) &&
                (k.Regelung == null || x.Regelung == k.Regelung) &&
                (k.Bauart == null || x.Bauart == k.Bauart) &&
                (k.Aufstellung == null || x.Aufstellung == k.Aufstellung) &&
                (k.Zuheizung == null || ZuheizungText(x.ElZuheizung) == k.Zuheizung) &&
                (x.MaxVorlauf >= k.VorlaufMin && x.MaxVorlauf <= k.VorlaufMax) &&
                (x.MaxLeistung >= k.LeistungMin && x.MaxLeistung <= k.LeistungMax) &&
                (ohneSuche || (x.Bezeichnung != null && muster.IsMatch(x.Bezeichnung)))
            ).ToList();
        }

        /// <summary>
        /// Die Werte EINER Klappliste: alles, was in den Zeilen vorkommt, ohne Leerwerte,
        /// ohne Dubletten, aufsteigend sortiert. Das Anhaengsel „Alle" gehoert der
        /// Oberflaeche und steht hier nicht mit drin (siehe Klassenkommentar).
        /// </summary>
        public static IReadOnlyList<string> Werte(
            IReadOnlyList<WaermepumpenKatalogZeile> zeilen,
            Func<WaermepumpenKatalogZeile, string> merkmal)
        {
            if (zeilen == null || merkmal == null) return Array.Empty<string>();
            return zeilen.Select(merkmal)
                         .Where(s => !string.IsNullOrEmpty(s))
                         .Distinct()
                         .OrderBy(s => s)
                         .ToList();
        }

        /// <summary>
        /// Die Klappliste „Zuheizung" fuehrt ZAHLEN als Text — <c>ElZuheizung.ToString()</c>
        /// im Vorlaeufer. Die Umwandlung steht hier an EINER Stelle, damit Liste und
        /// Vergleich nicht auseinanderlaufen koennen.
        /// </summary>
        public static string ZuheizungText(double wert) => wert.ToString();

        /// <summary>Der groesste vorkommende Vorlauf — die Vorbelegung von „VLT Max".</summary>
        public static double GroessterVorlauf(IReadOnlyList<WaermepumpenKatalogZeile> zeilen)
            => (zeilen == null || zeilen.Count == 0) ? 0 : zeilen.Max(x => x.MaxVorlauf);

        /// <summary>Die groesste vorkommende Leistung — die Vorbelegung von „Leist. Max".</summary>
        public static double GroessteLeistung(IReadOnlyList<WaermepumpenKatalogZeile> zeilen)
            => (zeilen == null || zeilen.Count == 0) ? 0 : zeilen.Max(x => x.MaxLeistung);
    }
}
