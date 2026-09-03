using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Einstellungen"/>: eine Ablage im
    /// Arbeitsspeicher, die mit dem Prozess endet.
    ///
    /// <para><b>Warum flüchtig und nicht leer.</b> Ein Prüfstand soll einen Wert setzen
    /// und ihn zurücklesen können, ohne eine Registry oder eine Datei anzufassen. Nichts
    /// bleibt zwischen zwei Läufen liegen — ein Test kann also keinen anderen
    /// beeinflussen, und ein Konsolenwerkzeug hinterlässt keine Spuren.</para>
    ///
    /// <para><see cref="LiesMaschine"/> liefert immer die Vorgabe: Ein maschinenweit
    /// gesetzter Abschalter ist ohne Betriebssystemablage nicht darstellbar, und „nicht
    /// abgeschaltet" ist der unauffällige Zustand.</para>
    /// </summary>
    public sealed class FluechtigeEinstellungen : IEinstellungen
    {
        private readonly Dictionary<string, string> _werte = new Dictionary<string, string>();

        /// <inheritdoc/>
        public string Lies(string schluessel, string vorgabe = null)
        {
            if (string.IsNullOrEmpty(schluessel)) return vorgabe;
            lock (_werte)
            {
                string wert;
                return _werte.TryGetValue(schluessel, out wert) ? wert : vorgabe;
            }
        }

        /// <inheritdoc/>
        public int LiesZahl(string schluessel, int vorgabe = 0)
        {
            string text = Lies(schluessel, null);
            int n;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : vorgabe;
        }

        /// <inheritdoc/>
        public void Schreib(string schluessel, string wert)
        {
            if (string.IsNullOrEmpty(schluessel)) return;
            lock (_werte) { _werte[schluessel] = wert; }
        }

        /// <inheritdoc/>
        public void SchreibZahl(string schluessel, int wert)
        {
            Schreib(schluessel, wert.ToString(CultureInfo.InvariantCulture));
        }

        /// <inheritdoc/>
        public void Loesche(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return;
            lock (_werte) { _werte.Remove(schluessel); }
        }

        /// <inheritdoc/>
        public string LiesMaschine(string schluessel, string vorgabe = null)
        {
            return vorgabe;
        }
    }
}
