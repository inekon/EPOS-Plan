namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die beiden Bedarfsrechnungen EINES Projekts — Wärme und Strom
    /// (iU9-W16b.4, Entscheid E-5, Befund W16-B29).
    ///
    /// <para><b>Warum es diese Klasse gibt.</b> <c>Form_Start</c> besaß die zwei
    /// Objekte als Felder (<c>simulationWaermebedarf</c>,
    /// <c>simulationStrombedarf</c>): Der Reiter „Simulation" rechnete damit die
    /// Zusammenfassung, und die Ergebnisansicht bekam sie HEREINGEREICHT und schrieb
    /// sie weiter. Genau das war der ausdrückliche Grund, warum die Ergebnisansicht
    /// ein MODALES Fenster bleiben musste (Befund W11-B3): Nebeneinander offen wären
    /// Startmaske und Ergebnisfenster über dieselben zwei Objekte im Streit
    /// gewesen.</para>
    ///
    /// <para><b>Was sich ändert.</b> Die zwei Objekte gehören jetzt dem PROJEKT und
    /// nicht mehr einem Fenster. Wechselt das Projekt, sind sie hinfällig und werden
    /// verworfen — vorher blieb ein einmal gerechneter Bedarf über den
    /// Projektwechsel hinweg stehen und wurde erst beim nächsten Betreten des
    /// Reiters überschrieben. Damit ist die Bedingung für E-5 erfüllt: Die
    /// Ergebnisansicht braucht kein zweites Fenster mehr, sie erscheint als
    /// <c>Ueberlagerung</c> derselben Seite.</para>
    ///
    /// <para><b>Die Objekte werden AN ORT UND STELLE fortgeschrieben</b> — dieselbe
    /// Mechanik wie im Bestand: Die Ergebnisansicht rechnet in sie hinein, und die
    /// Zusammenfassung des Reiters liest denselben Stand.</para>
    /// </summary>
    public sealed class BedarfsZustand
    {
        private int _idProjekt;
        private SimulationWaermebedarf _waerme;
        private SimulationStrombedarf _strom;

        /// <summary>Das Projekt, zu dem die beiden Rechnungen gehören; <c>0</c> = keins.</summary>
        public int ProjektId { get { return _idProjekt; } }

        /// <summary>
        /// Die Wärmebedarfsrechnung des Projekts. Sie entsteht beim ersten Zugriff
        /// und wird bei einem Projektwechsel verworfen.
        /// </summary>
        public SimulationWaermebedarf Waerme
        {
            get { return _waerme ?? (_waerme = new SimulationWaermebedarf()); }
        }

        /// <summary>Die Strombedarfsrechnung des Projekts — dieselbe Lebensdauer.</summary>
        public SimulationStrombedarf Strom
        {
            get { return _strom ?? (_strom = new SimulationStrombedarf()); }
        }

        /// <summary>
        /// Stellt den Zustand auf ein Projekt ein. Ein WECHSEL verwirft beide
        /// Rechnungen; derselbe Aufruf mit derselben Id lässt sie stehen.
        /// </summary>
        public void FuerProjekt(int idProjekt)
        {
            if (_idProjekt == idProjekt) return;

            _idProjekt = idProjekt;
            _waerme = null;
            _strom = null;
        }
    }
}
