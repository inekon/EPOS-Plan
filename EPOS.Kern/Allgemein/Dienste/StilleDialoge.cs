using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Dialog"/>: Meldungen gehen auf die
    /// Konsole, Rückfragen werden verneint.
    ///
    /// <para><b>Wortgleich zu <see cref="Meldung"/>.</b> Die drei Meldungsformen
    /// schreiben Zeile für Zeile dasselbe wie die bisherigen Vorbelegungen der
    /// Melde-Haken — im Referenzlauf landet die Meldung damit unverändert im
    /// Laufprotokoll statt in einem Dialog, auf den niemand klickt.</para>
    ///
    /// <para><b>Warum <see cref="Frage"/> „Nein" antwortet.</b> Ohne Bedienung wird die
    /// Variante mit dem KLEINEREN Schaden gewählt: „Nein" heißt, der angebotene Vorgang
    /// unterbleibt — nichts wird gelöscht, nichts überschrieben, nichts aktiv gesetzt.
    /// Das steht NICHT im Widerspruch zu <c>AnlagenEindeutigkeit.Fragen</c>, das im
    /// Engine-Modus mit „Ja" antwortet: Dort ist „Nein" die schädlichere Antwort, weil
    /// sie eine Anlagenzeile still verwirft, während „Ja" sie behält und eine eigene
    /// Gerätekopie anlegt. Beide Stellen folgen derselben Regel, kommen wegen ihrer
    /// unterschiedlichen Fachlage aber zu unterschiedlichen Antworten — und
    /// <c>AnlagenEindeutigkeit</c> entscheidet ohnehin VOR dieser Vorbelegung, weil es
    /// den Engine-Modus zuerst prüft.</para>
    ///
    /// <para><see cref="Wahl"/> antwortet aus demselben Grund mit
    /// <see cref="JaNeinAbbruch.Abbruch"/> und <see cref="Warten"/> bleibt folgenlos —
    /// eine Sanduhr gibt es ohne Oberfläche nicht.</para>
    /// </summary>
    public sealed class StilleDialoge : IDialogDienst
    {
        /// <inheritdoc/>
        public void Meldung(string text, string titel = null)
        {
            if (titel == null) Schreib(text);
            else Schreib((titel ?? "") + ": " + text);
        }

        /// <inheritdoc/>
        public void Warnung(string text, string titel = null)
        {
            Schreib("WARNUNG - " + (titel ?? "") + ": " + text);
        }

        /// <inheritdoc/>
        public void Fehler(string text, string titel = null)
        {
            Schreib("FEHLER - " + (titel ?? "") + ": " + text);
        }

        /// <inheritdoc/>
        public bool Frage(string text, string titel = null, bool warnend = false, bool vorgabeNein = false)
        {
            Schreib("FRAGE - " + (titel ?? "") + ": " + text + " - ohne Bedienung: nein.");
            return false;
        }

        /// <inheritdoc/>
        public JaNeinAbbruch Wahl(string text, string titel = null)
        {
            Schreib("WAHL - " + (titel ?? "") + ": " + text + " - ohne Bedienung: Abbruch.");
            return JaNeinAbbruch.Abbruch;
        }

        /// <inheritdoc/>
        public void Warten(bool an)
        {
        }

        private static void Schreib(string zeile)
        {
            try { Console.WriteLine(zeile); } catch { }
        }
    }
}
