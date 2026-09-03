using System.Globalization;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Sprache"/>: hält <see cref="Sprache"/>
    /// und die Anzeigekultur des Laufs zusammen.
    ///
    /// <para><b>Was <see cref="Setzen"/> tut.</b> Dreierlei, und zwar dasselbe wie
    /// bisher <c>Program.Main</c>: die Kennnummer in <see cref="Sprache.Nummer"/>, die
    /// Anzeigekultur des laufenden Fadens und — neu seit iU5 — die Vorgabe für alle
    /// weiteren Fäden (<see cref="CultureInfo.DefaultThreadCurrentUICulture"/>). Ohne
    /// die dritte Zuweisung beantwortet ein Hintergrundfaden Textabrufe in der Sprache
    /// des Betriebssystems statt in der eingestellten; im Bestand fiel das nicht auf,
    /// weil die Oberfläche einfädig arbeitet.</para>
    ///
    /// <para>Die Rechenkultur (<c>CurrentCulture</c>) wird ABSICHTLICH nicht angefasst —
    /// sie entscheidet über Zahlenformate und gehört nicht zur Oberflächensprache
    /// (Drei-Schichten-Regel, Konzept 13.6).</para>
    /// </summary>
    public class StandardSprache : ISprache
    {
        /// <summary>Kürzel für Deutsch.</summary>
        public const string DE = "de";

        /// <summary>Kürzel für Englisch.</summary>
        public const string EN = "en";

        /// <inheritdoc/>
        public string Kuerzel
        {
            get { return Sprache.Englisch ? EN : DE; }
        }

        /// <inheritdoc/>
        public bool IstEnglisch
        {
            get { return Sprache.Englisch; }
        }

        /// <inheritdoc/>
        public virtual void Setzen(string kuerzel)
        {
            KulturUebernehmen(IstEnglischesKuerzel(kuerzel));
        }

        /// <summary>
        /// <c>true</c>, wenn das Kürzel Englisch meint. Alles, was nicht mit
        /// <c>"en"</c> beginnt — auch <c>null</c> —, gilt als Deutsch; das entspricht
        /// dem bisherigen <c>nLanguage == 0</c>-Zweig.
        /// </summary>
        protected static bool IstEnglischesKuerzel(string kuerzel)
        {
            return kuerzel != null &&
                   kuerzel.StartsWith(EN, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Setzt Kennnummer und Anzeigekultur. Die Windows-Fassung ruft das, nachdem sie
        /// den Registry-Wert geschrieben hat.
        /// </summary>
        protected static void KulturUebernehmen(bool englisch)
        {
            Sprache.Nummer = englisch ? 1 : 0;

            CultureInfo kultur = new CultureInfo(englisch ? "en-US" : "de-DE");
            CultureInfo.DefaultThreadCurrentUICulture = kultur;
            Thread.CurrentThread.CurrentUICulture = kultur;
        }
    }
}
