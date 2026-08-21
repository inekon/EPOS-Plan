using System;
using System.Drawing;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Uebersetzt die sprachneutralen Protokollschluessel der Engine
    /// (<see cref="PruefMeldung"/>) in Anzeigetexte. Drei-Schichten-Regel: die
    /// Engine liefert Schluessel und Werte, der Text kommt ausschliesslich aus
    /// <c>MyResource</c>.
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

        /// <summary>Farbe einer Pruefstufe in der Protokollliste.</summary>
        /// <param name="stufe">Stufe.</param>
        public static Color StufeFarbe(PruefStufe stufe)
        {
            switch (stufe)
            {
                case PruefStufe.Fehler: return Color.FromArgb(176, 0, 32);
                case PruefStufe.Warnung: return Color.FromArgb(160, 96, 0);
                default: return SystemColors.WindowText;
            }
        }
    }
}
