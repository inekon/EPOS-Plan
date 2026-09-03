using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ERSATZ fuer <c>Allgemein\Bericht\BerichtTexte.cs</c> (Paket iU7-3).
    ///
    /// <para>Der Renderer ruft an genau EINER Stelle <c>BerichtTexte.T("Jahr")</c> -
    /// die Beschriftung der x-Achse des Kapitalwert-Diagramms. Die Bestandsdatei
    /// laesst sich hier nicht verlinken: Ihre Eigenschaft <c>Englisch</c> liest
    /// <c>Program.nLanguage</c>, und <c>Program</c> ist die WinForms-Anwendung.
    /// Sie deswegen umzubauen kam nicht in Frage - die Sprachumschaltung des
    /// Berichts ist keine Baustelle dieses Pakets.</para>
    ///
    /// <para>Die Probe zeichnet deutsch, also gibt <c>T</c> den Eingabetext
    /// unveraendert zurueck - dasselbe, was die Bestandsfassung bei deutscher
    /// Oberflaeche tut.</para>
    /// </summary>
    internal static class BerichtTexte
    {
        public static bool Englisch { get { return false; } }

        public static CultureInfo Kultur { get { return CultureInfo.GetCultureInfo("de-DE"); } }

        public static string T(string de) { return de; }
    }
}
