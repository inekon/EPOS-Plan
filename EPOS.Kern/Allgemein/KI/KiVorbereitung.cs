// Ergebnis der Vorbereitung einer bestaetigungspflichtigen Assistentenaktion.
//
// Die Klasse lag bis iU9-W15b in WindowsFormsApplication1\Allgemein\KI\KiAusfuehrer.cs.
// Sie ist mit dem Umzug von KiChatService in den Kern hierher gewandert: Der Dienst
// gibt sie aus KiAusfuehrungsweg.VorbereitenAsync zurueck, und der Kern darf keinen
// Typ aus der Windows-Anwendung kennen. Der Inhalt ist unveraendert - zwei Verweise
// aus KiKern und eine abgeleitete Frage.

using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das Ergebnis der VORBEREITUNG einer bestaetigungspflichtigen Aktion
    /// (Fachkonzept 3.5, Punkte 1 und 2).
    /// </summary>
    /// <remarks>
    /// Entweder liegt eine offene <see cref="Freigabe"/> vor - dann ist alles geprueft,
    /// die Vorschau erzeugt und der Sicherungspunkt angelegt, und es fehlt nur noch der
    /// Klick. Oder es liegt eine <see cref="Ablehnung"/> vor; die ist dann bereits
    /// protokolliert und geht als <c>functionResponse</c> an das Modell zurueck.
    /// </remarks>
    public sealed class KiVorbereitung
    {
        internal KiVorbereitung(KiFreigabe freigabe, KiErgebnis ablehnung)
        {
            Freigabe = freigabe;
            Ablehnung = ablehnung;
        }

        /// <summary>Die offene Freigabe; <c>null</c>, wenn abgelehnt wurde.</summary>
        public KiFreigabe Freigabe { get; }

        /// <summary>Die Ablehnung; <c>null</c>, wenn die Vorbereitung gelungen ist.</summary>
        public KiErgebnis Ablehnung { get; }

        /// <summary>Liegt eine offene Freigabe vor?</summary>
        public bool Bereit => Freigabe != null;
    }
}
