using System;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Pfade"/>: die Ordner des Betriebssystems
    /// über <see cref="Environment.SpecialFolder"/>, mit ZEICHENGLEICHEN Unterordnern
    /// zum Bestand.
    ///
    /// <para><b>Warum das schon ohne Adapter richtig ist.</b>
    /// <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/> gibt es auf
    /// jeder Plattform; unter Windows liefert es dieselben Pfade wie bisher, unter Linux
    /// und macOS die dortigen Entsprechungen (<c>~/.config</c>, <c>~/.local/share</c>).
    /// Die Windows-Fassung <c>WindowsPfade</c> muss deshalb nur eine Angabe wirklich
    /// überschreiben: <see cref="Produktdaten"/>, das im Bestand über
    /// <c>Application.ProductName</c> gebildet wird und damit an WinForms hängt.</para>
    /// </summary>
    public class StandardPfade : IPfade
    {
        /// <summary>Der Unterordner unter <c>%APPDATA%</c> — klein geschrieben, gewachsener Bestand.</summary>
        protected const string OrdnerKlein = "wp-plan";

        /// <summary>Der Unterordner unter den übrigen Wurzeln, und der Rückfall für <see cref="Produktdaten"/>.</summary>
        protected const string OrdnerGross = "WP-Plan";

        /// <inheritdoc/>
        public virtual string Anwendungsdaten
        {
            get { return Path.Combine(Wurzel(Environment.SpecialFolder.ApplicationData), OrdnerKlein); }
        }

        /// <inheritdoc/>
        public virtual string Produktdaten
        {
            get { return Path.Combine(Wurzel(Environment.SpecialFolder.ApplicationData), OrdnerGross); }
        }

        /// <inheritdoc/>
        public virtual string BenutzerLokal
        {
            get { return Path.Combine(BenutzerLokalBasis, OrdnerGross); }
        }

        /// <inheritdoc/>
        public virtual string BenutzerLokalBasis
        {
            get { return Wurzel(Environment.SpecialFolder.LocalApplicationData); }
        }

        /// <inheritdoc/>
        public virtual string Gemeinsam
        {
            get { return Path.Combine(Wurzel(Environment.SpecialFolder.CommonApplicationData), OrdnerGross); }
        }

        /// <inheritdoc/>
        public virtual string Dokumente
        {
            get { return Wurzel(Environment.SpecialFolder.MyDocuments); }
        }

        /// <inheritdoc/>
        public string Verbinde(string wurzel, params string[] teile)
        {
            string pfad = wurzel ?? "";
            if (teile != null)
            {
                for (int i = 0; i < teile.Length; i++)
                {
                    if (string.IsNullOrEmpty(teile[i])) continue;
                    pfad = Path.Combine(pfad, teile[i]);
                }
            }
            return pfad;
        }

        /// <inheritdoc/>
        public string Unterordner(string wurzel, params string[] teile)
        {
            string pfad = Verbinde(wurzel, teile);
            try { if (!string.IsNullOrEmpty(pfad)) Directory.CreateDirectory(pfad); } catch { }
            return pfad;
        }

        private static string Wurzel(Environment.SpecialFolder ordner)
        {
            try { return Environment.GetFolderPath(ordner) ?? ""; } catch { return ""; }
        }
    }
}
