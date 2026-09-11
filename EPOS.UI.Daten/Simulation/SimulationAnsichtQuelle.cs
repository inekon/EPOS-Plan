using System;
using System.Collections.Generic;
using System.Globalization;

using EPOS.UI.Seiten.Simulation;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE QUELLE der Ansicht „Simulation" (Auftrag #208, Stufe S2 des
    /// Konzepts „Simulationsablauf ohne Dialog") — der Ersatz für die Windows-Hülle
    /// <c>Views/Simulation/SimulationHuelle</c> aus #207.
    ///
    /// <para><b>Sie rechnet nichts und liest nichts.</b> Die zwei Datenseiten stehen
    /// unverändert in <see cref="SimulationKonfigHuelle"/> und
    /// <see cref="SimulationErgebnisHuelle"/>; hier werden ihre zwei Parametersätze zu
    /// EINEM Wörterbuch der Ansicht zusammengefasst und die zwei Auskünfte beigelegt,
    /// die die Ablaufleiste braucht: der Sperrgrund (rote Vorprüfung) und „gibt es
    /// einen gerechneten Lauf?" (Schritt ③).</para>
    ///
    /// <para><b>Warum sie die INSTANZEN hält.</b> Der gerechnete Lauf, die zwölf Bilder
    /// und die Gültigkeitsmarke leben in der <see cref="SimulationErgebnisHuelle"/>,
    /// der ungespeicherte Kaskadenstand in der <see cref="SimulationKonfigHuelle"/> —
    /// beides überlebt einen Ansichtswechsel nur, wenn jemand die Hülle festhält. Bis
    /// #207 tat das niemand, und deshalb ging der Rückweg aus der
    /// Stromspeicher-Auslegung ins Leere (Konzept 1.3).</para>
    ///
    /// <para><b>Eine Hülle JE PROJEKT.</b> Wechselt das Projekt, entsteht eine neue —
    /// der alte Lauf gehörte dem alten Projekt. Dieselbe Regel, nach der
    /// <c>BedarfsZustand.FuerProjekt</c> die zwei Bedarfsrechnungen verwirft.</para>
    ///
    /// <para><b>Was die Schale beisteuert</b>, steht in
    /// <see cref="SimulationPlattformwege"/> — heute genau ein Weg (der
    /// Wärmepumpen-Assistent, der ein Fenster braucht). Windows legt ihn ein, iOS legt
    /// seinen benannten Sperrgrund ein; alles andere ist auf beiden Plattformen
    /// derselbe Code.</para>
    /// </summary>
    internal sealed class SimulationAnsichtQuelle
    {
        private readonly BedarfsZustand _bedarf;
        private readonly SimulationPlattformwege _wege;

        /// <summary>Das Projekt, zu dem die zwei gehaltenen Hüllen gehören; 0 = keines.</summary>
        private int _idProjekt;

        private SimulationKonfigHuelle _konfig;
        private SimulationErgebnisHuelle _ergebnis;

        /// <param name="bedarf">
        /// Die zwei Bedarfsrechnungen des offenen Projekts (Befund W16-B29, Entscheid
        /// E-5). Sie gehören dem PROJEKT und werden unter Windows mit der Startseite
        /// geteilt — deren Reiter „Simulation" rechnet dieselben Zahlen.
        /// </param>
        /// <param name="wege">
        /// Die Naht zur Schale; <c>null</c> = diese Schale bietet keinen der Wege an
        /// (dann lehnt die Hülle sie benannt ab, still fällt nichts aus).
        /// </param>
        internal SimulationAnsichtQuelle(BedarfsZustand bedarf, SimulationPlattformwege wege)
        {
            _bedarf = bedarf ?? throw new ArgumentNullException(nameof(bedarf));
            _wege = wege ?? new SimulationPlattformwege();
        }

        // =====================================================================
        //  Der Parametersatz der Ansicht
        // =====================================================================

        /// <summary>
        /// Der Parametersatz der Ansicht SIMULATION — EIN Wörterbuch mit den zwei
        /// Parametersätzen darin. <c>null</c> = es ist kein Projekt offen.
        /// </summary>
        /// <param name="idProjekt"><c>Tab_Projekt.ID</c>; 0 = kein Projekt.</param>
        /// <param name="projektName">
        /// Der Projektname für die Kopfzeile; leer = ohne Zeile.
        /// </param>
        internal IReadOnlyDictionary<string, object> AnsichtGaben(int idProjekt, string projektName)
        {
            if (idProjekt <= 0) return null;

            Nachziehen(idProjekt);

            return new Dictionary<string, object>
            {
                ["Dienste"] = new SimulationAnsichtDienste
                {
                    Konfiguration = _konfig.Gaben(),
                    Ergebnis = _ergebnis.Gaben(),
                    Sperrgrund = SimulationErgebnisHuelle.Sperrgrund,
                    ErgebnisVorhanden = () => _ergebnis.LaufGerechnet,
                    // AUFTRAG #216: Die fuenf Laufparameter stehen in Schritt 1, ihre Delegaten kommen
                    // weiterhin aus der ERGEBNISHUELLE — sie haelt die zwei Felder, mit denen der Lauf
                    // bestueckt wird (Betriebsart, Leistungsgrenze). Beim Merge #208 hierher gezogen.
                    Parameter = _ergebnis.ParameterGaben()
                },
                ["ProjektText"] = Projektzeile(projektName)
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
            _ergebnis = SimulationErgebnisHuelle.Erzeugen(_wege, idProjekt, _bedarf);
        }

        /// <summary>„Projekt „…"" — die Kopfzeile der Ansicht; leer ohne Namen.</summary>
        private static string Projektzeile(string name)
        {
            return string.IsNullOrWhiteSpace(name)
                ? ""
                : string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.FLOTTE_SEITE_PROJEKT, name);
        }
    }
}
