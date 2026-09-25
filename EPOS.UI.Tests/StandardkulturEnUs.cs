using System.Globalization;
using System.Runtime.CompilerServices;

namespace EPOS.UI.Tests;

/// <summary>
/// Die feste Standardkultur dieses Testprojekts: <b>en-US</b>, gesetzt beim Laden der
/// Testassembly, bevor der erste Fall läuft (Auftrag #529, CI-Wächter). Gegenstück zu
/// <c>EPOS.Kern.Tests/StandardkulturEnUs.cs</c>; dort steht die Begründung ausführlich.
///
/// <para><b>Kurz.</b> Der Windows-Läufer der CI läuft unter en-US, ubuntu unter der invarianten
/// Kultur, die die neutralen — deutschen — Ressourcen liefert. Ein Fall, der deutsche Texte
/// erwartet und nicht pinnt, fiel deshalb erst auf dem Windows-Läufer. Mit en-US als Standard
/// fällt er auf jedem Rechner, schon im lokalen Gate. Wer deutsche Texte oder Zahlformate
/// erwartet, erbt von <see cref="EposBunitContext"/> oder legt eine
/// <see cref="Kulturvorrichtung"/> an; sie stellt danach wieder auf en-US zurück.</para>
///
/// <para>Der Kulturwächter (<c>EPOS.Kern.Tests/KulturwaechterTests.cs</c>) nimmt genau diese
/// Datei von der Rückstell-Pflicht aus und verlangt, dass sie besteht und en-US setzt.</para>
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
