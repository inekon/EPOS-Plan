#nullable enable

namespace EPOS.UI.Dialoge.Solarthermie
{
    /// <summary>
    /// Wie der Import einer Solarthermie-Ganglinie ausgegangen ist (iU9-W14b.2).
    ///
    /// <para>Der Vorläufer entschied das an drei Stellen mit drei
    /// <c>MessageBox</c>: „bereits vorhanden", „Fehler beim Speichern" und der
    /// stille Ausstieg bei leerem Dateinamen. Hier kommt es als EIN Ergebnis
    /// zurück, und der Dialog macht daraus ein Warnbanner.</para>
    /// </summary>
    public sealed class SolarganglinieImportErgebnis
    {
        /// <summary>Steht die Ganglinie im Katalog?</summary>
        public bool Erfolgreich;

        /// <summary>Der Bezeichner, unter dem sie steht (der Dateiname ohne Endung).</summary>
        public string Bezeichner = "";

        /// <summary>Der fertige Text für das Banner; leer heißt: nichts zu melden.</summary>
        public string Meldung = "";

        /// <summary>
        /// Ist die Meldung ein FEHLER? Sonst ist sie eine Erfolgsmeldung
        /// (<see cref="Erfolgreich"/>) oder ein Hinweis.
        /// </summary>
        public bool IstFehler;
    }
}
