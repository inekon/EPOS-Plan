using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

using EPOS.UI.Seiten.Simulation;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Ansicht „Simulation" (Auftrag #207, Stufe S1 des
    /// Konzepts „Simulationsablauf ohne Dialog") — die Plattformseite von
    /// <c>EPOS.UI/Seiten/Simulation/SimulationSeite.razor</c>.
    ///
    /// <para><b>Sie rechnet nichts und liest nichts.</b> Die zwei Datenseiten stehen
    /// unverändert in <see cref="SimulationKonfigHuelle"/> und
    /// <see cref="SimulationErgebnisHuelle"/>; hier werden ihre zwei
    /// Parametersätze zu EINEM Wörterbuch der Ansicht zusammengefasst und die zwei
    /// Auskünfte beigelegt, die die Ablaufleiste braucht: der Sperrgrund (rote
    /// Vorprüfung) und „gibt es einen gerechneten Lauf?" (Schritt ③).</para>
    ///
    /// <para><b>Warum sie die INSTANZEN hält.</b> Der gerechnete Lauf, die zwölf
    /// Bilder und die Gültigkeitsmarke leben in der
    /// <see cref="SimulationErgebnisHuelle"/>, der ungespeicherte Kaskadenstand in
    /// der <see cref="SimulationKonfigHuelle"/> — beides überlebt einen
    /// Ansichtswechsel nur, wenn jemand die Hülle festhält. Bis #207 tat das
    /// niemand: <c>StartseiteHuelle</c> baute bei JEDEM Kachelklick eine neue
    /// (das statische <c>SimulationErgebnisHuelle.Gaben(…)</c>, seither gefallen),
    /// und deshalb ging der Rückweg aus
    /// der Stromspeicher-Auslegung ins Leere — der Nachzug
    /// (<c>_flotteProjektGeaendert</c>, <c>_ergebnisGueltig = false</c>) traf eine
    /// Hülle, deren Seite nicht mehr stand (Konzept 1.3).</para>
    ///
    /// <para><b>Eine Hülle JE PROJEKT.</b> Wechselt der Projektkontext, entsteht eine
    /// neue — der alte Lauf gehörte dem alten Projekt. Dieselbe Regel, nach der
    /// <c>BedarfsZustand.FuerProjekt</c> die zwei Bedarfsrechnungen verwirft.</para>
    ///
    /// <para><b>Muster:</b> <c>StromspeicherAuslegungHuelle.AnsichtGaben</c> (#192)
    /// und <c>AssistentHuelle.AnsichtGaben</c> (#62b) — ein DELEGAT je Betreten statt
    /// eines stehenden Wörterbuchs, weil der Satz den Stand der Hülle mitbringt.</para>
    /// </summary>
    internal sealed class SimulationHuelle
    {
        private readonly Func<Form> _besitzer;
        private readonly ProjektKontextCtrl _kontext;
        private readonly BedarfsZustand _bedarf;

        /// <summary>Das Projekt, zu dem die zwei gehaltenen Hüllen gehören; 0 = keine.</summary>
        private int _idProjekt;

        private SimulationKonfigHuelle _konfig;
        private SimulationErgebnisHuelle _ergebnis;

        /// <param name="besitzer">
        /// Das Fenster, über dem die Unterdialoge und die Dateiwähler der
        /// CSV-Ausgaben erscheinen — die Ansicht hat kein eigenes (Entscheid E-5).
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
            _besitzer = besitzer ?? throw new ArgumentNullException(nameof(besitzer));
            _kontext = kontext ?? throw new ArgumentNullException(nameof(kontext));
            _bedarf = bedarf ?? throw new ArgumentNullException(nameof(bedarf));
        }

        // =====================================================================
        //  Der Parametersatz der Ansicht
        // =====================================================================

        /// <summary>
        /// Der Parametersatz der Ansicht SIMULATION — EIN Wörterbuch mit den zwei
        /// Parametersätzen darin. <c>null</c> = es ist kein Projekt offen.
        /// </summary>
        internal IReadOnlyDictionary<string, object> AnsichtGaben()
        {
            int id = _kontext.Id;
            if (id <= 0) return null;

            Nachziehen(id);

            return new Dictionary<string, object>
            {
                ["Dienste"] = new SimulationAnsichtDienste
                {
                    Konfiguration = _konfig.Gaben(),
                    Ergebnis = _ergebnis.Gaben(),
                    Sperrgrund = SimulationErgebnisHuelle.Sperrgrund,
                    ErgebnisVorhanden = () => _ergebnis.LaufGerechnet,

                    // AUFTRAG #216: Die fünf Laufparameter stehen in Schritt ①, ihre
                    // Delegaten kommen aber weiterhin aus der ERGEBNISHÜLLE — sie
                    // hält die zwei Felder, mit denen der Lauf bestückt wird
                    // (Betriebsart, Leistungsgrenze). Ein eigener Weg über die
                    // Konfigurationshülle schriebe dieselben Spalten und ließe diese
                    // Felder stehen.
                    Parameter = _ergebnis.ParameterGaben()
                },
                ["ProjektText"] = Projektzeile()
            };
        }

        /// <summary>
        /// Legt die zwei Hüllen an, sobald das Projekt gewechselt hat — und nur dann.
        /// </summary>
        private void Nachziehen(int idProjekt)
        {
            if (_idProjekt == idProjekt && _konfig != null && _ergebnis != null) return;

            _idProjekt = idProjekt;
            _konfig = SimulationKonfigHuelle.Erzeugen(idProjekt);
            _ergebnis = SimulationErgebnisHuelle.Erzeugen(_besitzer, idProjekt, _bedarf);
        }

        /// <summary>„Projekt „…"" — die Kopfzeile der Ansicht; leer ohne Namen.</summary>
        private string Projektzeile()
        {
            string name = _kontext.Name;
            return string.IsNullOrWhiteSpace(name)
                ? ""
                : string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.FLOTTE_SEITE_PROJEKT, name);
        }
    }
}
