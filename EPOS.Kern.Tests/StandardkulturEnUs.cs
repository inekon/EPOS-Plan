using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die feste Standardkultur dieses Testprojekts: <b>en-US</b>, gesetzt beim Laden der
    /// Testassembly, bevor der erste Fall läuft (Auftrag #531, CI-Wächter; Befunde #515 und
    /// #525). Gegenstück: <c>EPOS.UI.Tests/StandardkulturEnUs.cs</c>.
    ///
    /// <para><b>Warum.</b> Der Windows-Läufer der CI (<c>windows.yml</c>) läuft unter en-US,
    /// ubuntu (<c>kern.yml</c>) unter der invarianten Kultur, der Arbeitsrechner unter de-DE.
    /// Die invariante Kultur liefert die NEUTRALEN Ressourcen, und die sind deutsch
    /// (<c>EPOS.Kern/MyResource/Resource.resx</c>); erst en-US greift auf
    /// <c>Resource.en-US.resx</c> durch. Ein Fall, der deutsche Ressourcentexte festhält, ohne die
    /// Kultur zu pinnen, war deshalb lokal und auf ubuntu grün und fiel erst auf dem
    /// Windows-Läufer — nach dem Push (#515 <c>GebaeudeHochrechnungTests</c>, #525
    /// <c>PvPreisProjektTests</c>). Mit en-US als Standard fällt jeder solche Fall auf JEDEM
    /// Rechner, schon im lokalen Gate.</para>
    ///
    /// <para><b>Regel für die Fälle.</b> Wer deutsche Texte oder deutsche Zahlformate erwartet,
    /// legt eine <see cref="Kulturvorrichtung"/> an — als Feld einer Klasse mit
    /// <c>IDisposable</c> oder mit <c>using var kultur = new Kulturvorrichtung();</c> im Fall. Sie
    /// stellt danach wieder auf en-US zurück.</para>
    ///
    /// <para>Der Kulturwächter (<c>KulturwaechterTests</c>) nimmt genau diese Datei und ihr
    /// Gegenstück von der Rückstell-Pflicht aus und verlangt zugleich, dass beide bestehen und
    /// en-US setzen. Der Hinweis CA2255 (Modulinitialisierer in einer Bibliothek) trifft hier
    /// nicht: Diese Assembly wird nur vom Testlauf geladen.</para>
    /// </summary>
    internal static class StandardkulturEnUs
    {
        /// <summary>Die Standardkultur der Testläufe.</summary>
        internal const string Kultur = "en-US";

#pragma warning disable CA2255 // Modulinitialisierer: gewollt, nur der Testlauf lädt diese Assembly.
        /// <summary>Setzt en-US prozessweit und für den ladenden Thread.</summary>
        [ModuleInitializer]
        internal static void Setzen()
        {
            CultureInfo enUs = CultureInfo.GetCultureInfo(Kultur);
            CultureInfo.DefaultThreadCurrentCulture = enUs;
            CultureInfo.DefaultThreadCurrentUICulture = enUs;
            Thread.CurrentThread.CurrentCulture = enUs;
            Thread.CurrentThread.CurrentUICulture = enUs;
        }
#pragma warning restore CA2255
    }
}
