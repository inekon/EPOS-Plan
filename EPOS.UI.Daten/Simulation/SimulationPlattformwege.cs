using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die NAHT zwischen der plattformfreien Simulationsansicht und der Schale, in
    /// der sie läuft (Auftrag #208, Stufe S2 des Konzepts „Simulationsablauf ohne
    /// Dialog").
    ///
    /// <para><b>Wozu sie da ist.</b> Von den 5 490 Zeilen der zwei Simulationshüllen
    /// waren genau SECHS Windows: ein <c>Func&lt;Form&gt;</c> als Fensterbesitzer und
    /// der eine Weg, der ihn braucht — der Wärmepumpen-Assistent
    /// (<c>WaermepumpenHuelle.Gaben(IWin32Window, …)</c> und der Nachzug über
    /// <c>WizardCtrl</c>). Alles andere geht seit Paket iU5 über <c>Dienste.*</c> und
    /// ist damit auf jeder Plattform derselbe Weg. Statt der sechs Zeilen wegen die
    /// ganze Datenseite in der Windows-Anwendung zu lassen, kommen sie als benannte
    /// Naht herein.</para>
    ///
    /// <para><b>Kein Delegat ist kein Knopf</b> (Hausregel der Auslegungsansicht) —
    /// mit einer Ausnahme, die der Auftrag ausdrücklich verlangt: Ein Weg, den die
    /// Plattform NICHT anbietet, wird BENANNT abgelehnt und fällt nicht still aus.
    /// Dafür trägt jede Naht ihren <see cref="WaermepumpeSperrgrund"/>; die Hülle
    /// meldet ihn über <c>Dienste.Dialog</c>, wenn jemand den Weg doch anstößt.</para>
    /// </summary>
    internal sealed class SimulationPlattformwege
    {
        /// <summary>
        /// Der Parametersatz des Wärmepumpendialogs (Doppelklick auf eine Modulzeile
        /// im Reiter „Wärmepumpe"). <c>null</c> = diese Plattform kennt den Weg nicht;
        /// dann gilt <see cref="WaermepumpeSperrgrund"/>.
        /// </summary>
        /// <remarks>
        /// Unter Windows ist das <c>WaermepumpenHuelle.Gaben(_fenster, projektId,
        /// modelle, wizard: false)</c> — sie öffnet aus dem Dialog heraus weitere
        /// WinForms-Fenster und braucht deshalb einen Fensterbesitzer.
        /// </remarks>
        internal Func<int, List<WErzeugerModel>, IReadOnlyDictionary<string, object>> WaermepumpeGaben;

        /// <summary>
        /// Der Nachzug nach dem Übernehmen: die Wärmeerzeuger des Projekts neu
        /// schreiben (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> /
        /// <c>Add_WP_Waermeerzeuger</c>). <c>null</c> = wie oben.
        /// </summary>
        internal Action<int, List<WErzeugerModel>> WaermepumpeUebernehmen;

        /// <summary>
        /// Der Grund, aus dem der Wärmepumpenweg auf DIESER Plattform nicht geht —
        /// die benannte Ablehnung. Leer = er geht.
        /// </summary>
        internal string WaermepumpeSperrgrund = "";

        /// <summary>Die Naht einer Schale, die keinen der Wege anbietet.</summary>
        internal static SimulationPlattformwege Ohne(string sperrgrund)
            => new SimulationPlattformwege { WaermepumpeSperrgrund = sperrgrund ?? "" };
    }
}
