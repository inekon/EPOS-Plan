using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace KiKern.Tests
{
    /// <summary>
    /// Die feste Standardkultur dieses Testprojekts: <b>en-US</b>, gesetzt beim Laden der
    /// Testassembly, bevor der erste Fall läuft — dieselbe Kultur wie der Windows-Läufer der CI.
    /// Muster und Begründung stehen in <c>EPOS.Kern.Tests/StandardkulturEnUs.cs</c> (Auftrag
    /// #531); für dieses Projekt vorsorglich nachgezogen mit Nachlese #534.
    ///
    /// <para><b>Regel für die Fälle.</b> Wer deutsche Texte oder deutsche Zahlformate erwartet,
    /// pinnt de-DE selbst und stellt in <c>Dispose()</c> oder <c>finally</c> zurück. Der
    /// Kulturwächter (<c>EPOS.Kern.Tests/KulturwaechterTests</c>) nimmt diese Datei nach Pfad
    /// von der Rückstell-Pflicht aus und verlangt zugleich, dass sie besteht und en-US setzt.
    /// CA2255 (Modulinitialisierer in einer Bibliothek) trifft hier nicht: Diese Assembly wird
    /// nur vom Testlauf geladen.</para>
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
