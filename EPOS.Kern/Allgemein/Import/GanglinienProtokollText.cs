using System;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Uebersetzt die sprachneutralen Protokollschluessel der Engine
    /// (<see cref="PruefMeldung"/>) in Anzeigetexte. Drei-Schichten-Regel: die
    /// Engine liefert Schluessel und Werte, der Text kommt ausschliesslich aus
    /// <c>MyResource</c>.
    ///
    /// <para><b>Seit iU9-W12.0c im Kern.</b> Vier Aufrufstellen in zwei Masken
    /// haengen daran, und beide werden in dieser Welle Razor-Komponenten; die
    /// Uebersetzung ist ausserdem oberflaechenfrei — bis auf die eine Farbe, die
    /// hier nicht mehr steht (siehe <see cref="StufeKlasse"/>).</para>
    /// </summary>
    public static class GanglinienProtokollText
    {
        /// <summary>
        /// Anzeigetext einer Meldung. Fehlt der Schluessel im Katalog, wird die
        /// sprachneutrale Kurzfassung angezeigt - besser als ein leeres Feld.
        /// </summary>
        /// <param name="m">Meldung.</param>
        public static string Text(PruefMeldung m)
        {
            if (m == null) return "";

            string vorlage = null;
            try
            {
                vorlage = MyResource.Resource.ResourceManager.GetString(m.Schluessel, MyResource.Resource.Culture);
            }
            catch (Exception) { }

            if (string.IsNullOrEmpty(vorlage)) return m.ToString();
            if (m.Werte.Length == 0) return vorlage;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, vorlage, m.Werte);
            }
            catch (FormatException)
            {
                return vorlage + " (" + string.Join("; ", m.Werte) + ")";
            }
        }

        /// <summary>Anzeigetext einer Pruefstufe.</summary>
        /// <param name="stufe">Stufe.</param>
        public static string StufeText(PruefStufe stufe)
        {
            switch (stufe)
            {
                case PruefStufe.Fehler: return MyResource.Resource.IMPORT_STUFE_FEHLER;
                case PruefStufe.Warnung: return MyResource.Resource.IMPORT_STUFE_WARNUNG;
                default: return MyResource.Resource.IMPORT_STUFE_INFO;
            }
        }

        /// <summary>
        /// Die CSS-Klasse einer Protokollzeile — der Ersatz fuer die frueheren
        /// <c>Color.FromArgb</c>-Werte (<c>176,0,32</c> fuer Fehler, <c>160,96,0</c>
        /// fuer Warnung, sonst <c>SystemColors.WindowText</c>).
        ///
        /// <para><b>Warum keine Farbe mehr.</b> <c>System.Drawing</c> ist im Kern
        /// verboten, und eine Farbe ist ohnehin eine Darstellungsentscheidung: Die
        /// Stufe sagt, WAS gilt; welcher Farbklang, welches Symbol und welcher
        /// Kontrastmodus daraus wird, entscheidet die Oberflaeche. Die beiden
        /// Zahlenwerte stehen unveraendert in <c>epos-ui.css</c> als
        /// <c>--epos-stufe-fehler</c> und <c>--epos-stufe-warnung</c>.</para>
        /// </summary>
        /// <param name="stufe">Stufe.</param>
        /// <returns><c>epos-stufe--fehler</c>, <c>epos-stufe--warnung</c> oder <c>epos-stufe--info</c>.</returns>
        public static string StufeKlasse(PruefStufe stufe)
        {
            switch (stufe)
            {
                case PruefStufe.Fehler: return "epos-stufe--fehler";
                case PruefStufe.Warnung: return "epos-stufe--warnung";
                default: return "epos-stufe--info";
            }
        }
    }
}
