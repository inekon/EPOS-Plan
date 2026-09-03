using System;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das SUCHMUSTER der Katalogsuchfelder (iU9-W9.0e) — <c>*</c> steht für beliebig
    /// viele, <c>?</c> für genau ein Zeichen.
    ///
    /// <para><b>Wozu.</b> Dieselbe Übersetzung stand zweimal im Bestand:
    /// <c>Form_WpFilterAuswahl.ApplyFilter</c> (seit iU9-W7.0b in
    /// <see cref="WaermepumpenKatalogFilter"/>) und
    /// <c>Form_Gebaeude.ApplyGridFilter</c> (:637-684). Beide bauen aus der Eingabe
    /// denselben regulären Ausdruck; sie unterschieden sich nur darin, wie sie das
    /// Ergebnis anwenden. Der gemeinsame Teil steht jetzt hier — ein Suchmuster im
    /// Haus.</para>
    ///
    /// <para><b>Drei Regeln, wörtlich aus beiden Vorläufern:</b></para>
    /// <list type="bullet">
    ///   <item>MIT Platzhalter wird das Muster verankert (<c>^…$</c>) — „Haus*" trifft
    ///     also nur, was mit „Haus" beginnt.</item>
    ///   <item>OHNE Platzhalter ist es eine Teilsuche wie <c>Contains</c>.</item>
    ///   <item>Groß- und Kleinschreibung spielt keine Rolle; ein Muster, an dem der
    ///     reguläre Ausdruck zerbricht, ist KEIN Filter (der Anwender tippt gerade, und
    ///     eine halb geschriebene Klammer darf die Liste nicht leeren).</item>
    /// </list>
    /// </summary>
    public static class Suchmuster
    {
        /// <summary>
        /// Übersetzt eine Eingabe in einen regulären Ausdruck. <c>null</c> heißt
        /// „kein Filter" — bei leerer Eingabe, bei <c>"*"</c> und bei einem Muster, das
        /// sich nicht übersetzen lässt.
        /// </summary>
        public static Regex Uebersetzen(string eingabe)
        {
            string muster = (eingabe ?? "").Trim();
            if (muster.Length == 0 || muster == "*") return null;

            try
            {
                // Regex.Escape maskiert alle Sonderzeichen; die maskierten Platzhalter
                // bekommen danach ihre Regex-Bedeutung zurueck.
                string ausdruck;
                if (muster.IndexOf('*') < 0 && muster.IndexOf('?') < 0)
                    ausdruck = Regex.Escape(muster);                       // Teilsuche
                else
                    ausdruck = "^" + Regex.Escape(muster)
                                          .Replace("\\*", ".*")
                                          .Replace("\\?", ".") + "$";      // verankert

                return new Regex(ausdruck, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            catch
            {
                return null;   // ungueltiges Muster -> Anzeige unveraendert lassen
            }
        }

        /// <summary>
        /// Trifft die Eingabe auf den Text? Ohne Muster (siehe <see cref="Uebersetzen"/>)
        /// trifft alles.
        ///
        /// <para>Zeilenumbrüche im Text werden vorher zu Leerzeichen — die Katalogzelle
        /// der Gebäudeverwaltung trägt „Art\\nFläche [m²]" in EINER Zelle
        /// (<c>ApplyGridFilter</c>:677).</para>
        /// </summary>
        public static bool Trifft(Regex muster, string text)
        {
            if (muster == null) return true;
            if (text == null) return false;
            return muster.IsMatch(text.Replace("\r", " ").Replace("\n", " "));
        }
    }
}
