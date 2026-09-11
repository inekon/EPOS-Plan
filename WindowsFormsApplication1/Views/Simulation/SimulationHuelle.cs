using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-SCHALE der Ansicht „Simulation" — seit Auftrag <b>#208</b> nur noch
    /// ein ADAPTER.
    ///
    /// <para><b>Was hier bis #208 stand</b> (Auftrag #207): das Halten der zwei
    /// Hülleninstanzen, das Zusammenfassen ihrer Parametersätze und die Projektzeile.
    /// Das ist alles plattformfrei und liegt seither in
    /// <c>EPOS.UI.Daten/Simulation/SimulationAnsichtQuelle.cs</c>; sonst wäre die
    /// Simulation auf iOS unerreichbar geblieben, obwohl von den 5 490 Zeilen der zwei
    /// Datenhüllen genau sechs Windows waren.</para>
    ///
    /// <para><b>Was hier geblieben ist</b>, ist genau das Windows-Stück: der
    /// FENSTERBESITZER. Der Wärmepumpen-Assistent öffnet aus sich heraus weitere
    /// WinForms-Fenster (<c>WaermepumpenHuelle.Gaben(IWin32Window, …)</c>) und braucht
    /// deshalb eines, über dem er erscheinen kann — die Ansicht selbst hat keines
    /// (Entscheid E-5). Er geht als benannter Weg in die Quelle
    /// (<c>SimulationPlattformwege</c>); der Nachzug über <c>WizardCtrl</c> ist
    /// Kern-Arbeit und liegt deshalb weiterhin in der Datenhülle.</para>
    ///
    /// <para><b>Muster:</b> <c>StromspeicherAuslegungHuelle.AnsichtGaben</c> (#192) und
    /// <c>AssistentHuelle.AnsichtGaben</c> (#62b) — ein DELEGAT je Betreten statt eines
    /// stehenden Wörterbuchs, weil der Satz den Stand der Hülle mitbringt.</para>
    /// </summary>
    internal sealed class SimulationHuelle
    {
        private readonly ProjektKontextCtrl _kontext;
        private readonly SimulationAnsichtQuelle _quelle;

        /// <param name="besitzer">
        /// Das Fenster, über dem die Unterdialoge des Wärmepumpen-Assistenten
        /// erscheinen — die Ansicht hat kein eigenes (Entscheid E-5).
        /// </param>
        /// <param name="kontext">Das offene Projekt (K2).</param>
        /// <param name="bedarf">
        /// Die zwei Bedarfsrechnungen des offenen Projekts (Befund W16-B29,
        /// Entscheid E-5). Sie gehören dem PROJEKT und werden mit der Startseite
        /// geteilt — deren Reiter „Simulation" rechnet dieselben Zahlen.
        /// </param>
        internal SimulationHuelle(Func<Form> besitzer, ProjektKontextCtrl kontext,
                                  BedarfsZustand bedarf)
        {
            if (besitzer == null) throw new ArgumentNullException(nameof(besitzer));
            _kontext = kontext ?? throw new ArgumentNullException(nameof(kontext));

            _quelle = new SimulationAnsichtQuelle(bedarf, new SimulationPlattformwege
            {
                WaermepumpeGaben = (idProjekt, modelle) =>
                    WaermepumpenHuelle.Gaben(besitzer(), idProjekt, modelle, wizard: false)
            });
        }

        /// <summary>
        /// Der Parametersatz der Ansicht SIMULATION; <c>null</c> = kein Projekt offen.
        /// </summary>
        internal IReadOnlyDictionary<string, object> AnsichtGaben()
            => _quelle.AnsichtGaben(_kontext.Id, _kontext.Name);
    }
}
