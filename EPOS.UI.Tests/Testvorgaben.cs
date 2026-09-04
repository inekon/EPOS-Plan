using System.Runtime.CompilerServices;
using Bunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Vorgaben, die fuer JEDEN bunit-Fall des Projekts gelten, gesetzt bevor der erste
/// Fall laeuft.
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
#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void Setzen()
    {
        BunitContext.DefaultWaitTimeout = TimeSpan.FromSeconds(10);
    }
#pragma warning restore CA2255
}
