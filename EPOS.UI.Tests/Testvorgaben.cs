using System.Runtime.CompilerServices;
using Bunit;
using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Vorgaben, die fuer JEDEN bunit-Fall des Projekts gelten, gesetzt bevor der erste
/// Fall laeuft - und die eine Zeitgrenze, die ein Fall an eine eigene Wartestelle
/// legt (<see cref="MitZeitgrenze"/>).
///
/// <para><b>Wartezeit zehn Sekunden statt einer.</b> bunit wartet in
/// <c>WaitForAssertion</c>/<c>WaitForState</c> in der Vorgabe eine Sekunde auf das
/// naechste Zeichnen. Auf dem ausgelasteten CI-Laeufer (1 900 Faelle parallel, zwei
/// Kerne) ist das zweimal gerissen, ohne dass sich am Dialog etwas geaendert hatte:
/// Kern-Lauf 33866130448 (<c>KlimadatenDialogTests</c>, Check count 0) und
/// Kern-Lauf 33870556040 (<c>ProjektTransferDialogTests</c>, Check count 1) - beide
/// Male auf reinen Dokumentations-Commits, beide Male lokal und im Wiederholungslauf
/// gruen. Ein ausdruecklicher Timeout an jeder Wartestelle (22 ohne, 6 mit) waere
/// dieselbe Aussage an 28 Orten; hier steht sie einmal. Eine Wartestelle mit eigenem
/// Timeout behaelt ihn.</para>
///
/// <para>CA2255 warnt, weil ein Modulinitialisierer in Bibliotheken ungewoehnlich ist;
/// ein Testprojekt ist die Ausnahme, die die Regel meint - der Initialisierer muss vor
/// dem ersten Testfall laufen, und xUnit bietet dafuer keinen frueheren Haken.</para>
/// </summary>
internal static class Testvorgaben
{
    /// <summary>
    /// Die Wartezeit, die fuer JEDE Wartestelle des Projekts gilt - die bunit-Vorgabe
    /// oben und die Zeitgrenze von <see cref="MitZeitgrenze"/>. EINE Zahl, damit ein
    /// ausgelasteter Laeufer nicht an zwei verschiedenen Grenzen scheitert.
    /// </summary>
    internal static readonly TimeSpan Wartezeit = TimeSpan.FromSeconds(10);

#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Setzen()
    {
        BunitContext.DefaultWaitTimeout = Wartezeit;
    }
#pragma warning restore CA2255

    /// <summary>
    /// Wartet hoechstens <see cref="Wartezeit"/> auf einen Rueckruf, statt unbegrenzt.
    /// </summary>
    /// <remarks>
    /// <para><b>Warum eine Grenze.</b> Ein Fall, der auf die
    /// <c>TaskCompletionSource</c> eines Rueckrufs wartet, haengt fuer immer, wenn der
    /// Klick daneben geht - aus einem roten Test wird ein Dauerlauf, der den ganzen
    /// Lauf blockiert (Befund DL-2f). Mit der Grenze faellt er binnen zehn Sekunden
    /// und sagt dabei, WELCHER Rueckruf ausgeblieben ist.</para>
    /// </remarks>
    /// <param name="warten">Die Aufgabe des Rueckrufs - meist <c>quelle.Task</c>.</param>
    /// <param name="rueckruf">Der Name des Rueckrufs fuer die Meldung.</param>
    internal static async Task MitZeitgrenze(this Task warten, string rueckruf)
    {
        try
        {
            await warten.WaitAsync(Wartezeit);
        }
        catch (TimeoutException)
        {
            Assert.Fail(
                "Der Rueckruf \"" + rueckruf + "\" ist binnen " +
                Wartezeit.TotalSeconds.ToString("0") +
                " s nicht zurueckgekommen - vermutlich ging der Klick daneben.");
        }
    }
}
