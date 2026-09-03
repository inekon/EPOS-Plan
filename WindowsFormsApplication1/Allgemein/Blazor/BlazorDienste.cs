using System;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das Dienstverzeichnis der Blazor-Huelle (Umsetzungskonzept iOS, Paket iU8).
    ///
    /// <para><b>Wozu.</b> Eine <c>BlazorWebView</c> braucht einen
    /// <see cref="IServiceProvider"/>: Aus ihm holt sie ihre eigenen Bausteine
    /// (<c>AddWindowsFormsBlazorWebView</c>) und aus ihm holen die Komponenten das,
    /// was sie per <c>@inject</c> anfordern. Fuer EPOS.UI ist das genau ein Dienst -
    /// der Zugang zum Hilfesystem.</para>
    ///
    /// <para><b>Warum trotzdem kein DI-Container fuer die Anwendung.</b> Das
    /// Verzeichnis endet an der Huelle. Die Umgebungsdienste des Kerns liegen
    /// weiterhin im statischen Halter <see cref="Dienste"/> (iU5); ein zweiter,
    /// konkurrierender Weg waere die Stelle, an der zwei Fassungen desselben
    /// Dienstes nebeneinander leben. Was eine Komponente aus der Umgebung braucht,
    /// bekommt sie deshalb entweder als Parameter von der Huelle oder ueber eine
    /// hier eingetragene Schnittstelle.</para>
    ///
    /// <para><b>Einmal, nicht je Dialog.</b> Der Aufbau kostet Zeit und die
    /// Blazor-Bausteine sind zustandslos; das Verzeichnis wird deshalb beim ersten
    /// Dialog gebaut und danach wiederverwendet.</para>
    /// </summary>
    internal static class BlazorDienste
    {
        private static readonly object _sperre = new object();

        private static IServiceProvider _dienste;

        /// <summary>
        /// Liefert das Dienstverzeichnis der Huelle; beim ersten Aufruf wird es
        /// gebaut.
        /// </summary>
        internal static IServiceProvider Erzeugen()
        {
            if (_dienste != null) return _dienste;

            lock (_sperre)
            {
                if (_dienste == null)
                {
                    var sammlung = new ServiceCollection();

                    // Alles, was eine BlazorWebView selbst braucht (WebViewManager,
                    // JS-Laufzeit, Dateianbieter).
                    sammlung.AddWindowsFormsBlazorWebView();

                    // Der Zugang zum Hilfesystem fuer <InfoKnopf>.
                    //
                    // VORLAEUFIG die leere Fassung: Die Windows-Bruecke auf
                    // HelpCatalog entsteht im naechsten Schritt (iU8-7,
                    // Allgemein\Hilfe\WindowsHilfeDienst.cs) und tritt dann hier an
                    // ihre Stelle. Bis dahin bleibt der Infoknopf sichtbar, aber
                    // wirkungslos - das ist genau das Verhalten, das KeineHilfe
                    // zusichert.
                    sammlung.AddSingleton<IHilfeDienst, KeineHilfe>();

                    _dienste = sammlung.BuildServiceProvider();
                }

                return _dienste;
            }
        }
    }
}
