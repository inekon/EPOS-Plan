// Umbruch der Kurzbeschreibung des Hilfepopups (H11 7.6) - seit iU9-W15b.0e im Kern.
//
// Die Rechnung lag bis dahin als "internal static" in
// WindowsFormsApplication1\Views\Help\Form_HelpPopup.cs (:186-231). Ihr Kommentarkopf
// versprach seit H11 ausdruecklich die Pruefbarkeit ("internal statt private, damit der
// Pruefstand die Kappung ohne Bildschirm nachrechnen kann") - einen Zeugen gab es
// trotzdem nie (Befund W15b-B18). Weil Form_HelpPopup als einzige Maske des Pakets
// weder umgestellt noch geloescht wird (Entscheid E-2: sein Ersatz ist IHilfeDienst mit
// Windows- und iOS-Fassung), waere die Zeichenrechnung sonst bis iU11 ungeprueft
// liegengeblieben. Sie ist reine Zeichenarbeit - kein Grafikkontext, keine Maske - und
// gehoert damit in den Kern. Form_HelpPopup ruft von hier; die iOS-Fassung des
// Hilfedienstes kann dieselbe Kappung nutzen, sobald sie eine Beschreibung hat (iU11).
//
// Der Inhalt ist Zeichen fuer Zeichen der alte: dieselben zwei Konstanten, dieselbe
// Wortschleife, derselbe CRLF-Verbinder, dasselbe Auslassungszeichen.

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Umbruch der Kurzbeschreibung, wie ihn das Hilfepopup zeigt.
    /// </summary>
    public static class Kurzbeschreibung
    {
        /// <summary>Ziellaenge einer Zeile der Kurzbeschreibung, in Zeichen.</summary>
        public const int ZEICHEN = 70;

        /// <summary>Mehr als so viele Zeilen zeigt das Popup nicht.</summary>
        public const int ZEILEN = 2;

        /// <summary>
        /// Bricht die Kurzbeschreibung an Wortgrenzen auf hoechstens <see cref="ZEILEN"/>
        /// Zeilen zu je rund <see cref="ZEICHEN"/> Zeichen um. Was nicht mehr
        /// hineinpasst, endet mit einem Auslassungszeichen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Umgebrochen wird ueber die Zeichenzahl, nicht ueber die gemessene Textbreite.
        /// Das ist bewusst grob: Das Popup ist AutoSize, die Randklemmung in
        /// <c>Form_HelpPopup.Anzeigen</c> holt jede Breite wieder auf den Bildschirm, und
        /// eine Messung ueber <c>TextRenderer</c> braeuchte einen Grafikkontext an einer
        /// Stelle, die sonst ohne auskommt - und im Kern gar keinen haette.
        /// </para>
        /// <para>
        /// Ein einzelnes ueberlanges Wort wird NICHT getrennt - es bekommt seine Zeile und
        /// darf laenger sein. Getrennte Fachwoerter waeren schlimmer als eine zu lange
        /// Zeile.
        /// </para>
        /// <para>
        /// Der Verbinder ist fest <c>"\r\n"</c> und nicht <c>Environment.NewLine</c>: Die
        /// Zeichenkette geht in ein WinForms-Label, und das rechnet mit CRLF. Auf einer
        /// Plattform mit LF-Zeilenenden waere die Ausgabe sonst je nach Laeufer eine
        /// andere - der Zeuge T-7 haengt daran.
        /// </para>
        /// </remarks>
        public static string Umbrechen(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            // Der Katalog liefert bereits einzeilig; ein Umbruch aus einer alten
            // Sicherung wuerde die Zeilenrechnung sonst durcheinanderbringen.
            string flach = Regex.Replace(text, @"\s+", " ").Trim();
            if (flach.Length == 0) return "";

            string[] woerter = flach.Split(' ');
            var zeilen = new List<string>();
            var aktuell = new StringBuilder();

            int i = 0;
            for (; i < woerter.Length; i++)
            {
                string wort = woerter[i];

                if (aktuell.Length == 0)
                {
                    aktuell.Append(wort);
                }
                else if (aktuell.Length + 1 + wort.Length <= ZEICHEN)
                {
                    aktuell.Append(' ').Append(wort);
                }
                else if (zeilen.Count + 1 >= ZEILEN)
                {
                    // Die angefangene Zeile ist die letzte erlaubte - hier ist Schluss.
                    break;
                }
                else
                {
                    zeilen.Add(aktuell.ToString());
                    aktuell.Clear();
                    aktuell.Append(wort);
                }
            }

            // Rest uebrig? Dann wurde gekappt und das muss man sehen.
            if (i < woerter.Length) aktuell.Append('…');

            zeilen.Add(aktuell.ToString());

            return string.Join("\r\n", zeilen);
        }
    }
}
